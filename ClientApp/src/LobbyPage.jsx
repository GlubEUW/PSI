import { useState, useEffect, useRef } from "react";
import { GetGuestUser } from "./api/user";
import { useParams, useNavigate } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";
import TicTacToe from "./TicTacToe.jsx";
import Rps from "./Rps.jsx";

const gameComponents = {
   TicTacToe: TicTacToe,
   RockPaperScissors: Rps
   // Future games can be added here
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
   const [phase, setPhase] = useState("lobby"); // "lobby" or "game"
   const [gameType, setGameType] = useState("TicTacToe"); // Default game type

   useEffect(() => {
      if (connectedRef.current) return;
      connectedRef.current = true;

      document.title = "Lobby: " + code;
      let conn = null;

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

         const userData = await response.json();
         setUser(userData);

         const conn = new HubConnectionBuilder()
            .withUrl("http://localhost:5243/matchHub")
            .withAutomaticReconnect()
            .build();

         conn.on("MatchStarted", ({ gameType }) => {
            setGameType(gameType);
            setPhase("game");
         });


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

            const success = await conn.invoke("JoinMatch", code, userData.name);
            if (success) {
               setMessage(`Joined lobby ${code}`);
            }
            else {
               setMessage("Failed to join the lobby. Name might be taken.");
            }

            const names = await conn.invoke("GetPlayers", code);
            setPlayers(names);
         } catch (err) {
            setMessage("Connection failed.");
            console.error(`Connection failed:, ${err}`);
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
         await connection.invoke("StartMatch", selectedGameType, code);
      } catch (err) {
         console.error(err);
      }
   };

   if (phase === "game" && gameType) {
      const GameComponent = gameComponents[gameType];
      return <GameComponent gameID={code} playerName={user.name} />;
   }

   return (
      <div>
         <h2>Lobby {code}</h2>
         <p>Your name is: {user.name}</p>
         <p>{message}</p>

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