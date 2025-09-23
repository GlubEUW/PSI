import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";

function LobbyPage() {
  const { code } = useParams(); // get the code from URL
  const [connection, setConnection] = useState(null);
  const [playerName, setPlayerName] = useState("");
  const [players, setPlayers] = useState([]);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const connect = async () => {
      const conn = new HubConnectionBuilder()
        .withUrl("http://localhost:5243/matchmakinghub")
        .withAutomaticReconnect()
        .build();

      // Listen for new players
      conn.on("PlayerJoined", (name) => {
        setPlayers((prev) => [...prev, name]);
        setMessage(`${name} joined the lobby`);
      });

      // Listen for game start
      conn.on("GameStarted", () => setMessage("Game started!"));

      await conn.start();
      console.log("Connected to hub:", conn.connectionId);
      setConnection(conn);
    };

    connect();
  }, []);

  const createLobby = async () => {
    if (!connection || !playerName) return;
    try {
      await connection.invoke("CreateGame", playerName, code);
      setPlayers([playerName]);
      setMessage(`Lobby created with code ${code}`);
    } catch (err) {
      console.error(err);
    }
  };

  const joinLobby = async () => {
  if (!connection || !playerName) return;

  try {
    const success = await connection.invoke("JoinGame", code, playerName);
    if (success) {
      setMessage(`Joined lobby ${code}`);
    } else {
      setMessage("Lobby not found");
    }
  } catch (err) {
    console.error(err);
  }
};

  const startGame = async () => {
    if (!connection) return;
    try {
      await connection.invoke("StartGame", code);
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div>
      <h2>Lobby {code}</h2>
      <input
        type="text"
        placeholder="Enter your name"
        value={playerName}
        onChange={(e) => setPlayerName(e.target.value)}
      />
      <br /><br />
      <button onClick={createLobby}>Create Lobby</button>
      <button onClick={joinLobby}>Join Lobby</button>
      <button onClick={startGame}>Start Game</button>

      <p>{message}</p>

      <h3>Players in Lobby:</h3>
      <ul>
        {players.map((p, idx) => <li key={idx}>{p}</li>)}
      </ul>
    </div>
  );
}

export default LobbyPage;
