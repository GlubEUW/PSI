import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";

function LobbyPage() {
    const { code } = useParams();
    const [connection, setConnection] = useState(null);
    const [playerName, setPlayerName] = useState("");
    const [players, setPlayers] = useState([]);
    const [message, setMessage] = useState("");

    useEffect(() => {
        document.title = 'Lobby: ' + code;
        const connect = async () => {
            const conn = new HubConnectionBuilder()
                .withUrl("http://localhost:5243/matchmakinghub")
                .withAutomaticReconnect()
                .build();

            conn.on("PlayerJoined", ({ name, connectionId }) => {
                setPlayers((prev) => {
                    if (prev.some((p) => p.connectionId === connectionId)) return prev;
                    return [...prev, { name, connectionId }];
                });
                setMessage(`${name} joined the lobby`);
            });

            conn.on("MatchStarted", () => setMessage("Match started!"));

            await conn.start();
            console.log("Connected to hub:", conn.connectionId);
            setConnection(conn);
        };

        connect();
    }, []);

    const createLobby = async () => {
        if (!connection || !playerName) return;
        try {
            await connection.invoke("CreateMatch", playerName, code);
            setPlayers([{ name: playerName, connectionId: connection.connectionId }]);
            setMessage(`Lobby created with code ${code}`);
        } catch (err) {
            console.error(err);
        }
    };

    const joinLobby = async () => {
        if (!connection || !playerName) return;
        if (players.some((p) => p.connectionId === connection.connectionId)) {
            setMessage("You are already in this lobby!");
            return;
        }
        try {
            const success = await connection.invoke("JoinMatch", code, playerName);
            if (success) {
                setMessage(`Joined lobby ${code}`);
            } else {
                setMessage("Failed to join Lobby");
            }
        } catch (err) {
            console.error(err);
        }
    };

    const startMatch = async () => {
        if (!connection) return;
        try {
            await connection.invoke("StartMatch", code);
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
            <button onClick={startMatch}>Start Match</button>

            <p>{message}</p>

            <h3>Players in Lobby:</h3>
            <ul>
                {players.map((p, idx) => (
                    <li key={p.id ?? idx}>{p.name}</li>
                ))}
            </ul>
        </div>
    );
}

export default LobbyPage;
