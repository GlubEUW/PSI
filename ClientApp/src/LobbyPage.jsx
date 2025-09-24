import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";

function getRandomGuestName() {
    const randomNumber = Math.floor(1000 + Math.random() * 9000);
    return `Guest${randomNumber}`;
}

function LobbyPage() {
    const { code } = useParams();
    const [connection, setConnection] = useState(null);
    const [playerName, setPlayerName] = useState("");
    const [players, setPlayers] = useState([]);
    const [message, setMessage] = useState("");

    useEffect(() => {
        document.title = "Lobby: " + code;
        const name = getRandomGuestName();
        setPlayerName(name);

        const connect = async () => {
            const conn = new HubConnectionBuilder()
                .withUrl("http://localhost:5243/matchHub")
                .withAutomaticReconnect()
                .build();

            conn.on("MatchStarted", () => setMessage("Match started!"));

            conn.on("PlayersUpdated", (playersList) => {
                setPlayers(playersList.map((n) => ({ name: n })));
            });

            try {
                await conn.start();
                console.log("Connected to hub:", conn.connectionId);
                setConnection(conn);

                const success = await conn.invoke("JoinMatch", code, name);
                if (success) {
                    setMessage(`Joined lobby ${code}`);
                } else {
                    await conn.invoke("CreateMatch", code);
                    setMessage(`Lobby created with code ${code}`);
                }

                const playersList = await conn.invoke("GetPlayers", code);
                setPlayers(playersList.map((n) => ({ name: n })));
            } catch (err) {
                console.error("Connection failed:", err);
            }
        };

        connect();
    }, [code]);

    const createLobby = async (conn) => {
        if (!conn) return;
        try {
            await conn.invoke("CreateMatch", code);
            setMessage(`Lobby created with code ${code}`);
        } catch (err) {
            console.error(err);
        }
    };

    const joinLobby = async (conn) => {
        if (!conn || !playerName) return;
        if (players.some((p) => p.connectionId === conn.connectionId)) {
            setMessage("You are already in this lobby!");
            return;
        }
        try {
            const success = await conn.invoke("JoinMatch", code, playerName);
            if (success) {
                setMessage(`Joined lobby ${code}`);
            } else {
                setMessage("Failed to join Lobby");
            }

            const playersList = await conn.invoke("GetPlayers", code);
            setPlayers(playersList.map((name, idx) => ({ name, connectionId: idx.toString() })));
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
            <p>Your name is: {playerName}</p>
            <p>{message}</p>
            <button onClick={startMatch}>Start Match</button>

            <h3>Players in Lobby:</h3>
            <ul>
                {players.map((p, idx) => (
                    <li key={idx}>{p.name}</li>
                ))}
            </ul>
        </div>
    );
}

export default LobbyPage;
