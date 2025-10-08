import { useState, useEffect } from "react";
import { HubConnectionBuilder, HttpTransportType } from "@microsoft/signalr";

const choices = ["Rock", "Paper", "Scissors"];

function Rps({ gameID, playerName }) {
  const [connection, setConnection] = useState(null);
  const [game, setGame] = useState({ Players: {}, Result: null });
  const [choice, setChoice] = useState(null);

  useEffect(() => {
    if (!gameID || !playerName) return;

    const conn = new HubConnectionBuilder()
      .withUrl("http://localhost:5243/gameHub", {  // CHANGED: rpsHub -> gameHub
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .build();

    conn.on("GameUpdate", (updatedGame) => {
      setGame(updatedGame);
    });

    const startConnection = async () => {
      try {
        await conn.start();
        await conn.invoke("StartGame", gameID, "RockPaperScissors");  // CHANGED: Added game type
        setConnection(conn);
      } catch (err) {
        console.error("Failed to connect to game hub:", err);
      }
    };

    startConnection();

    return () => conn.stop();
  }, [gameID, playerName]);

  const makeMove = (choice) => {
    if (!connection) return;
    setChoice(choice);
    connection.invoke("MakeRpsMove", gameID, playerName, choice)  // CHANGED: MakeMove -> MakeRpsMove
      .catch(console.error);
  };

  return (
    <div>
      <h2>Rock Paper Scissors</h2>
      {game.Result ? <h3>{game.Result}</h3> : <h3>Your choice: {choice ?? "None"}</h3>}
      <div>
        {choices.map((c) => (
          <button key={c} onClick={() => makeMove(c)}>
            {c}
          </button>
        ))}
      </div>
      <h3>Players:</h3>
      <ul>
        {Object.entries(game.Players).map(([name, c]) => (
          <li key={name}>
            {name}: {c}
          </li>
        ))}
      </ul>
    </div>
  );
}

export default Rps;