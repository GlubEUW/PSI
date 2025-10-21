import { useState, useEffect, useRef } from "react";
import { GetGuestUser } from "./api/user";
import { useParams, useNavigate } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";
import TicTacToe from "./TicTacToe.jsx";
import Rps from "./Rps.jsx";
import TournamentPage from "./Tournament.jsx";

const gameComponents = {
   TicTacToe: TicTacToe,
   RockPaperScissors: Rps
};

function LobbyPage() {
   const token = localStorage.getItem("guestToken");
   const { code } = useParams();
   const navigate = useNavigate();
   const [user, setUser] = useState({ name: "Loading..." });
   const [connection, setConnection] = useState(null);
   const [players, setPlayers] = useState([]);
   const [message, setMessage] = useState("");

   const connectedRef = useRef(false);
   const [phase, setPhase] = useState("lobby");
   const [gameType, setGameType] = useState("TicTacToe");
   const [mode, setMode] = useState("match");

   useEffect(() => {
      if (connectedRef.current) return;
      connectedRef.current = true;

      document.title = "Lobby: " + code;
      let conn;

      const connect = async () => {
         if (!token) {
            setMessage("You must be logged in to access the lobby.");
            setTimeout(() => navigate("/start"), 3000);
            return;
         }

         const response = await GetGuestUser(token);
         if (!response.ok) {
            setMessage("Failed to fetch user info. Redirecting...");
            setTimeout(() => navigate("/home"), 3000);
            return;
         }

         const userData = await response.json();
         setUser(userData);

         conn = new HubConnectionBuilder()
            .withUrl(`http://localhost:5243/matchHub?code=${code}&playerName=${userData.name}`, {
               accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

         conn.on("PlayersUpdated", async () => {
            try {
               const names = await conn.invoke("GetPlayers", code);
               setPlayers(names);
            } catch {
               setPlayers([]);
            }
         });
         conn.on("MatchStarted", (data) => {
            console.log("Match started!", data);
            setGameType(data.gameType);
            setPhase("game");
         });


         try {
            await conn.start();
            setConnection(conn);
            setMessage(`Connected to lobby ${code}`);

            const names = await conn.invoke("GetPlayers", code);
            setPlayers(names);
         } catch (err) {
            console.error("Connection failed:", err);
            setMessage("Connection failed.");
         }
      };

      connect();

      return () => {
         if (conn) conn.stop();
      };
   }, [code]);

   const startMatch = async (selectedGameType) => {
      if (!connection) return;
      try {
         await connection.invoke("StartMatch", selectedGameType);
      } catch (err) {
         console.error(err);
      }
   };

   if (mode === "tournament") {
      return (
         <TournamentPage
            code={code}
            playerName={user.name}
            connection={connection}
         />
      );
   }

   if (phase === "game" && gameType) {
      const GameComponent = gameComponents[gameType];
      return <GameComponent
         gameId={code}
         playerName={user.name}
         connection={connection}
         onReturnToLobby={() => setPhase("lobby")}
      />;
   }

   return (
      <div>
         <h2>Lobby {code}</h2>
         <p>Your name is: {user.name}</p>
         <p>{message}</p>

         <div>
            <label>Mode:</label>
            <select value={mode} onChange={(e) => setMode(e.target.value)}>
               <option value="match">Quick Match</option>
               <option value="tournament">Tournament</option>
            </select>
         </div>

         <label>
            Game Type:
            <select
               value={gameType}
               onChange={(e) => setGameType(e.target.value)}
            >
               <option value="" disabled>Select a game</option>
               <option value="TicTacToe">Tic Tac Toe</option>
               <option value="RockPaperScissors">Rock Paper Scissors</option>
            </select>
         </label>

         <button onClick={() => startMatch(gameType)}>Start Match</button>

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