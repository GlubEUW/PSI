import { useState, useEffect } from "react";
import { GetGuestUser } from "./api/user";
import { useParams, useNavigate } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";

function LobbyPage() {
    const token = localStorage.getItem("guestToken");
    const { code } = useParams();
    const navigate = useNavigate();
    const [user, setUser] = useState({ name: "Loading..." });
    const [connection, setConnection] = useState(null);
    const [players, setPlayers] = useState([]);
    const [message, setMessage] = useState("");

    useEffect(() => {
        document.title = "Lobby: " + code;
        var conn = null;
        const connect = async () => {
            if (!token) {
                setMessage("You must be logged in to access the lobby.");
                setTimeout(() => navigate("/start"), 3000);
                return;
            }

            const response = await GetGuestUser(token);
            if (!response.ok) {
                setMessage("Failed to fetch user info. Redirecting to home page...");
                setUser({ name: "Failed." });
                setTimeout(() => navigate("/home"), 3000);
                return;
            }
            setUser(await response.json());

            conn = new HubConnectionBuilder()
                .withUrl("http://localhost:5243/matchHub")
                .withAutomaticReconnect()
                .build();

            conn.on("MatchStarted", () => setMessage("Match started!"));

            conn.on("PlayersUpdated", async () => {
                try {
                    const names = await conn.invoke("GetPlayers", code);
                    setPlayers(names);
                } catch (err) {
                    setPlayers([]);
                }
            });

            try {
                await conn.start();
                setConnection(conn);

                const success = await conn.invoke("JoinMatch", code, token);
                if (success) {
                    setMessage(`Joined lobby ${code}`);
                }

                const names = await conn.invoke("GetPlayers", code);
                setPlayers(names);
            } catch (err) {
                setMessage("Connection failed.");
                console.error("Connection failed:", err);
            }
        };

        connect();

        return () => {
            if (conn) conn.stop();
        };
    }, [code]);

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
            <p>Your name is: {user.name}</p>
            <p>{message}</p>
            <button onClick={startMatch}>Start Match</button>

            <h3>Players in Lobby:</h3>
            <ul>
                {players.map((name, idx) => (
                    <li key={idx}>{name}</li>
                ))}
            </ul>
        </div>
    );
}

export default LobbyPage;