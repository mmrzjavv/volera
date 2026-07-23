# Server setup and CI/CD (GitHub → your server)

This guide gets your server ready for deployments from GitHub Actions and configures GitHub secrets.

## 1. What to install on the server (185.164.72.106)

SSH into your server (use your password when prompted):

```bash
ssh root@185.164.72.106
```

Then run:

```bash
# Update and install Docker Compose plugin + Git (Docker you already have)
sudo apt update
sudo apt install -y git docker-compose-plugin

# Confirm Docker and Compose work
docker --version
docker compose version
```

You only need **Docker** (already installed), **Docker Compose** (`docker-compose-plugin`), and **Git**.

---

## 2. Clone the repo and create `.env`

On the server, choose a directory for the app (e.g. `/opt/chat-app`) and clone your repo:

```bash
sudo mkdir -p /opt/chat-app
sudo chown $USER:$USER /opt/chat-app
cd /opt/chat-app
git clone https://github.com/YOUR_USERNAME/Chat-App-DotNet.git .
```

Replace `YOUR_USERNAME` with your GitHub username (or use the full repo URL if it’s under an organization).

**If the repo is private**: the server must be able to `git pull` without typing a password. Either:
- Use an SSH deploy key: add the server’s SSH public key in GitHub (Repo → Settings → Deploy keys), then on the server run `git remote set-url origin git@github.com:YOUR_USERNAME/Chat-App-DotNet.git`, or  
- Use HTTPS with a Personal Access Token: `git clone https://YOUR_TOKEN@github.com/YOUR_USERNAME/Chat-App-DotNet.git`

Create the env file and set a strong PostgreSQL password:

```bash
cd backend-core
cp .env.example .env
nano .env   # set POSTGRES_PASSWORD=your_strong_password
```

Save and exit. Do **not** commit `.env` (it’s in `.gitignore`).

---

## 3. SSH key for GitHub Actions

GitHub Actions will log in to your server via SSH (key-based, no password).

**On your local machine** (PowerShell or WSL):

```bash
# Generate a key pair (no passphrase so the workflow can use it)
ssh-keygen -t ed25519 -C "github-actions-deploy" -f deploy_key -N ""
```

This creates `deploy_key` (private) and `deploy_key.pub` (public).

**On the server**, add the public key:

```bash
mkdir -p ~/.ssh
echo "PASTE_CONTENT_OF_deploy_key.pub_HERE" >> ~/.ssh/authorized_keys
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys
```

Paste the **entire** contents of `deploy_key.pub` (one line).

**In GitHub**, add the **private** key as a secret:

1. Repo → **Settings** → **Secrets and variables** → **Actions**
2. **New repository secret**
3. Name: `SSH_PRIVATE_KEY`
4. Value: entire content of `deploy_key` (including `-----BEGIN ...` and `-----END ...`)

Then add these repository secrets:

| Name                 | Value                     | Required |
|----------------------|---------------------------|----------|
| `SSH_PRIVATE_KEY`    | (private key content)     | Yes      |
| `SERVER_HOST`        | `185.164.72.106`          | Yes      |
| `SERVER_USER`        | `root` (or your SSH user) | Yes      |
| `SERVER_DEPLOY_PATH` | `/opt/chat-app`           | No (defaults to `/opt/chat-app`) |

After saving, delete the private key file from your local machine if you don’t need it elsewhere:

```bash
# Optional: remove from local machine after adding to GitHub
del deploy_key
del deploy_key.pub
```

---

## 4. First run on the server

SSH to the server and start the stack once (backend + frontend + PostgreSQL):

```bash
cd /opt/chat-app/backend-core
docker compose --env-file .env up -d --build
```

Check that the API and frontend are up:

```bash
curl -s http://localhost:5000/health
# Should return: "OK"

curl -sI http://localhost:80
# Should return 200 (frontend)
```

- **Frontend**: http://185.164.72.106 (port 80)
- **API (direct)**: http://185.164.72.106:5000 (optional; frontend proxies `/api` and hubs through port 80)

---

## 5. How CI/CD runs

- **On every push to `main`**: GitHub Actions builds the backend and frontend, runs backend tests, then SSHs to your server, runs `git pull` in the repo, and runs `docker compose up -d --build` from `backend-core` (this builds and starts backend, frontend, and PostgreSQL).
- **Manual run**: **Actions** → **Build and Deploy** → **Run workflow**.

After each deploy:
- **Frontend** is at **http://your-server** (port 80); it proxies `/api` and SignalR hubs to the backend.
- **Backend** is also reachable directly on port **5000** if needed.

---

## 6. Firewall (if you use one)

If you use `ufw` or another firewall, open SSH, the frontend (HTTP), optionally the API port, and **Coturn** (required for voice/video/screen share when users are not on the same LAN):

```bash
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 5000/tcp
# Coturn STUN/TURN (self-hosted WebRTC)
sudo ufw allow 3478/tcp
sudo ufw allow 3478/udp
sudo ufw allow 40000:40050/udp
sudo ufw enable
```

In `backend-core/.env` set the address browsers use to reach Coturn (usually the same as `SERVER_HOST`):

```bash
TURN_PUBLIC_HOST=185.164.72.106
TURN_EXTERNAL_IP=185.164.72.106
TURN_USERNAME=volera
TURN_CREDENTIAL=your_strong_turn_secret
COTURN_ENABLED=true
```

Then recreate: `docker compose --env-file .env up -d`. Clients load ICE servers from `GET /api/v1/Call/ice-servers` (no Google/Twilio).

---

## 7. Optional: Redis / MongoDB

The app can use Redis and MongoDB. Right now the compose file runs WebAPI, frontend, and Coturn (Postgres is external). To add Redis/MongoDB later, you can:

- Add more services to `backend-core/docker-compose.yml`, or
- Keep using your current Liara (or other) Redis/Mongo and set the connection strings in `appsettings.Production.json` or via environment variables on the server.

---

## Summary

| On server          | On GitHub (secrets)     |
|-------------------|-------------------------|
| Docker            | `SSH_PRIVATE_KEY`       |
| Docker Compose    | `SERVER_HOST`           |
| Git               | `SERVER_USER`           |
| Repo at `/opt/chat-app` | `SERVER_DEPLOY_PATH` (optional) |
| `.env` in `backend-core` from `.env.example` (incl. `TURN_PUBLIC_HOST`) | |

Do **not** put your server password or `.env` contents in GitHub; only the SSH private key and host/user are stored as secrets.
