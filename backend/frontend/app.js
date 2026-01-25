const API_BASE = "http://localhost:5001/api";
let token = null;
let currentUser = null;
let connection = null;
let peerConnection = null;
let localStream = null;
let currentCallId = null;
let callerId = null;

document.addEventListener("DOMContentLoaded", () => {
  setupEventListeners();
  checkAuth();
});

function setupEventListeners() {
  // Auth
  document.getElementById("loginForm").addEventListener("submit", handleLogin);
  document
    .getElementById("registerForm")
    .addEventListener("submit", handleRegister);
  document
    .getElementById("showRegister")
    .addEventListener("click", showRegister);
  document.getElementById("showLogin").addEventListener("click", showLogin);
  document.getElementById("logoutBtn").addEventListener("click", logout);

  // Calls
  document.getElementById("acceptCall").addEventListener("click", acceptCall);
  document.getElementById("rejectCall").addEventListener("click", rejectCall);
  document.getElementById("endCall").addEventListener("click", endCall);
}

function checkAuth() {
  token = localStorage.getItem("token");
  if (token) {
    showMain();
    startSignalR();
    loadUsers();
  } else {
    showAuth();
  }
}

function showAuth() {
  document.getElementById("auth").classList.remove("hidden");
  document.getElementById("register").classList.add("hidden");
  document.getElementById("main").classList.add("hidden");
}

function showRegister() {
  document.getElementById("auth").classList.add("hidden");
  document.getElementById("register").classList.remove("hidden");
}

function showLogin() {
  document.getElementById("register").classList.add("hidden");
  document.getElementById("auth").classList.remove("hidden");
}

function showMain() {
  document.getElementById("auth").classList.add("hidden");
  document.getElementById("register").classList.add("hidden");
  document.getElementById("main").classList.remove("hidden");
}

async function handleLogin(e) {
  e.preventDefault();
  const username = document.getElementById("username").value;
  const password = document.getElementById("password").value;

  try {
    const response = await fetch(`${API_BASE}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Username: username, Password: password }),
    });
    const data = await response.json();
    if (response.ok) {
      token = data.Token;
      currentUser = data.User;
      localStorage.setItem("token", token);
      showMain();
      startSignalR();
      loadUsers();
    } else {
      alert("Login failed");
    }
  } catch (error) {
    console.error("Login error:", error);
  }
}

async function handleRegister(e) {
  e.preventDefault();
  const firstName = document.getElementById("regFirstName").value;
  const lastName = document.getElementById("regLastName").value;
  const username = document.getElementById("regUsername").value;
  const phoneNumber = document.getElementById("regPhone").value;
  const password = document.getElementById("regPassword").value;

  try {
    const response = await fetch(`${API_BASE}/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        FirstName: firstName,
        LastName: lastName,
        Username: username,
        PhoneNumber: phoneNumber,
        Password: password,
      }),
    });
    if (response.ok) {
      alert("Registration successful. Please login.");
      showLogin();
    } else {
      alert("Registration failed");
    }
  } catch (error) {
    console.error("Register error:", error);
  }
}

function logout() {
  token = null;
  currentUser = null;
  localStorage.removeItem("token");
  if (connection) connection.stop();
  if (peerConnection) peerConnection.close();
  showAuth();
}

async function loadUsers() {
  try {
    const response = await fetch(`${API_BASE}/user`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (response.ok) {
      const users = await response.json();
      displayUsers(users);
    } else {
      console.error("Failed to load users:", response.status);
    }
  } catch (error) {
    console.error("Load users error:", error);
  }
}

function displayUsers(users) {
  const usersList = document.getElementById("users");
  usersList.innerHTML = "";
  users.forEach((user) => {
    const li = document.createElement("li");
    li.innerHTML = `
            <span>${user.firstName} ${user.lastName} <span class="${user.isOnline ? "online" : "offline"}">(${user.isOnline ? "Online" : "Offline"})</span></span>
            <button onclick="initiateCall('${user.id}')" ${!user.isOnline ? "disabled" : ""}>Call</button>
        `;
    usersList.appendChild(li);
  });
}

async function initiateCall(receiverId) {
  try {
    const response = await fetch(`${API_BASE}/call/initiate`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ receiverId }),
    });
    const data = await response.json();
    currentCallId = data.callId;
    await setupWebRTC();
    await connection.invoke("JoinCallGroup", currentCallId);
    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);
    await connection.invoke("SendOffer", currentCallId, JSON.stringify(offer));
  } catch (error) {
    console.error("Initiate call error:", error);
  }
}

async function acceptCall() {
  try {
    await fetch(`${API_BASE}/call/${currentCallId}/accept`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });
    document.getElementById("incomingCall").classList.add("hidden");
    document.getElementById("ongoingCall").classList.remove("hidden");
    await connection.invoke("JoinCallGroup", currentCallId);
    await setupWebRTC();
    const answer = await peerConnection.createAnswer();
    await peerConnection.setLocalDescription(answer);
    await connection.invoke(
      "SendAnswer",
      currentCallId,
      JSON.stringify(answer),
    );
  } catch (error) {
    console.error("Accept call error:", error);
  }
}

async function rejectCall() {
  try {
    await fetch(`${API_BASE}/call/${currentCallId}/reject`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });
    hideCallUI();
  } catch (error) {
    console.error("Reject call error:", error);
  }
}

async function endCall() {
  try {
    await fetch(`${API_BASE}/call/${currentCallId}/end`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
    });
    cleanupCall();
  } catch (error) {
    console.error("End call error:", error);
  }
}

function hideCallUI() {
  document.getElementById("incomingCall").classList.add("hidden");
  document.getElementById("ongoingCall").classList.add("hidden");
  currentCallId = null;
  callerId = null;
}

function cleanupCall() {
  if (peerConnection) {
    peerConnection.close();
    peerConnection = null;
  }
  if (localStream) {
    localStream.getTracks().forEach((track) => track.stop());
    localStream = null;
  }
  hideCallUI();
}

async function setupWebRTC() {
  peerConnection = new RTCPeerConnection();
  localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
  document.getElementById("localAudio").srcObject = localStream;
  localStream
    .getTracks()
    .forEach((track) => peerConnection.addTrack(track, localStream));

  peerConnection.ontrack = (event) => {
    document.getElementById("remoteAudio").srcObject = event.streams[0];
  };

  peerConnection.onicecandidate = (event) => {
    if (event.candidate) {
      connection.invoke(
        "SendIceCandidate",
        currentCallId,
        JSON.stringify(event.candidate),
      );
    }
  };
}

function startSignalR() {
  connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5001/callHub", {
      accessTokenFactory: () => token,
    })
    .build();

  connection.on("CallInitiated", (callId, callerId, receiverId) => {
    if (receiverId === currentUser.id) {
      currentCallId = callId;
      callerId = callerId;
      document.getElementById("callerName").textContent =
        `Incoming call from ${callerId}`;
      document.getElementById("incomingCall").classList.remove("hidden");
    }
  });

  connection.on("CallAccepted", (callId, callerId, receiverId) => {
    if (callerId === currentUser.id) {
      document.getElementById("ongoingCall").classList.remove("hidden");
      document.getElementById("callStatus").textContent =
        `On call with ${receiverId}`;
    }
  });

  connection.on("CallEnded", (callId, callerId, receiverId, duration) => {
    cleanupCall();
  });

  connection.on("ReceiveOffer", async (offer) => {
    await peerConnection.setRemoteDescription(
      new RTCSessionDescription(JSON.parse(offer)),
    );
  });

  connection.on("ReceiveAnswer", async (answer) => {
    await peerConnection.setRemoteDescription(
      new RTCSessionDescription(JSON.parse(answer)),
    );
  });

  connection.on("ReceiveIceCandidate", async (candidate) => {
    await peerConnection.addIceCandidate(
      new RTCIceCandidate(JSON.parse(candidate)),
    );
  });

  connection.start().catch((err) => console.error(err));
}
