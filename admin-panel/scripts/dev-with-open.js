/**
 * Finds a free port, starts Next.js dev server on it, then opens the browser when ready.
 * Avoids EADDRINUSE and auto-opens the admin panel.
 */
const { spawn } = require('child_process');
const http = require('http');
const net = require('net');

function findFreePort(startPort) {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.listen(startPort, () => {
      const { port } = server.address();
      server.close(() => resolve(port));
    });
    server.on('error', () => resolve(findFreePort(startPort + 1)));
  });
}

function waitForServer(url, maxWaitMs = 90000, intervalMs = 500) {
  const start = Date.now();
  return new Promise((resolve, reject) => {
    function tryOnce() {
      if (Date.now() - start > maxWaitMs) {
        reject(new Error('Server did not start in time'));
        return;
      }
      const req = http.get(url, (res) => {
        if (res.statusCode >= 200 && res.statusCode < 500) resolve();
        else setTimeout(tryOnce, intervalMs);
      });
      req.on('error', () => setTimeout(tryOnce, intervalMs));
      req.setTimeout(2000, () => {
        req.destroy();
        setTimeout(tryOnce, intervalMs);
      });
    }
    tryOnce();
  });
}

async function main() {
  const port = await findFreePort(3000);
  const url = `http://localhost:${port}`;
  console.log('Starting Next.js on', url, '...\n');

  const next = spawn('npx', ['next', 'dev', '-p', String(port)], {
    stdio: 'inherit',
    shell: true,
    env: process.env,
  });

  next.on('error', (err) => {
    console.error('Failed to start Next.js:', err);
    process.exit(1);
  });

  next.on('exit', (code) => {
    process.exit(code ?? 0);
  });

  // Open browser once server responds (after a short delay so Next can compile)
  setTimeout(async () => {
    try {
      await waitForServer(url);
      require('open')(url).catch(() => {});
    } catch {
      // Server might have exited; ignore
    }
  }, 4000);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
