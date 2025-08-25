let connection;
let currentRoom = null;
let username = "";

const el = id => document.getElementById(id);
const status = el("status");
const users = el("users");
const messages = el("messages");
const typing = el("typing");

function addMsg({ user, message, at }) {
  const div = document.createElement("div");
  div.className = "msg";
  const time = at ? new Date(at).toLocaleTimeString() : "";
  div.innerHTML = `<strong>${user}</strong>: ${message} <br><small>${time}</small>`;
  messages.appendChild(div);
  messages.scrollTop = messages.scrollHeight;
}

function setPresence(list) {
  users.innerHTML = "";
  list.forEach(u => {
    const li = document.createElement("li");
    li.textContent = u;
    users.appendChild(li);
  });
}

el("connectBtn").addEventListener("click", async () => {
  username = el("username").value.trim() || "Guest";
  connection = new signalR.HubConnectionBuilder()
    .withUrl(`/hubs/chat?user=${encodeURIComponent(username)}`)
    .withAutomaticReconnect()
    .build();

  // Handlers
  connection.on("Presence", setPresence);
  connection.on("UserJoined", u =>
    addMsg({ user: "system", message: `${u} joined` })
  );
  connection.on("UserLeft", u =>
    addMsg({ user: "system", message: `${u} left` })
  );
  connection.on("Message", addMsg);
  connection.on("RoomEvent", ({ room, type, user }) => {
    addMsg({ user: "system", message: `${user} ${type}ed room #${room}` });
  });
  connection.on("RoomMessage", addMsg);
  connection.on("Typing", u => {
    typing.textContent = `${u} is typing…`;
    clearTimeout(window.__typingTimer);
    window.__typingTimer = setTimeout(() => (typing.textContent = ""), 900);
  });

  connection.onreconnecting(() => (status.textContent = "reconnecting…"));
  connection.onreconnected(() => (status.textContent = "connected"));
  connection.onclose(() => {
    status.textContent = "disconnected";
    el("sendBtn").disabled = true;
    el("message").disabled = true;
    el("joinBtn").disabled = true;
    el("leaveBtn").disabled = true;
  });

  await connection.start();
  status.textContent = "connected";
  el("sendBtn").disabled = false;
  el("message").disabled = false;
  el("joinBtn").disabled = false;

  addMsg({ user: "system", message: `Welcome, ${username}!` });
});

el("sendBtn").addEventListener("click", async () => {
  const text = el("message").value.trim();
  if (!text) return;
  if (currentRoom) await connection.invoke("SendToRoom", currentRoom, text);
  else await connection.invoke("SendMessage", text);
  el("message").value = "";
});

el("message").addEventListener("input", async () => {
  if (!connection) return;
  await connection.invoke("Typing", currentRoom);
});

el("joinBtn").addEventListener("click", async () => {
  const room = el("room").value.trim();
  if (!room) return alert("Enter a room name");
  await connection.invoke("JoinRoom", room);
  currentRoom = room;
  el("leaveBtn").disabled = false;
});

el("leaveBtn").addEventListener("click", async () => {
  if (!currentRoom) return;
  await connection.invoke("LeaveRoom", currentRoom);
  currentRoom = null;
  el("leaveBtn").disabled = true;
});
