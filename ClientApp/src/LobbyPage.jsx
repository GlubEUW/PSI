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
   const token = localStorage.getItem("userToken");
   const { code } = useParams();
   const navigate = useNavigate();
   const [totalRounds, setTotalRounds] = useState(1);
   const [currentRound, setCurrentRound] = useState(1);
   const [user, setUser] = useState({ name: "Loading...", id: "" });
   const [connection, setConnection] = useState(null);
   const [players, setPlayers] = useState([{ name: "", wins: 0 }]);
   const [message, setMessage] = useState("");

   const connectedRef = useRef(false);
   const [phase, setPhase] = useState("lobby");
   const [gameType, setGameType] = useState("TicTacToe");

   useEffect(() => {
      if (connectedRef.current)
         return;

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
            .withUrl(`http://localhost:5243/matchHub?code=${code}`, {
               accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

         conn.on("PlayersUpdated", async (roundInfo) => {
            try {
               if (roundInfo) {
                  setCurrentRound(roundInfo.currentRound);
                  setTotalRounds(roundInfo.totalRounds);
               }

               const playerInfo = await conn.invoke("GetPlayers", code);
               setPlayers(playerInfo);
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

            const playerInfo = await conn.invoke("GetPlayers", code);
            setPlayers(playerInfo);
         } catch (err) {
            console.error("Connection failed:", err);
            setMessage("Connection failed.");
         }
      };

      connect();

      return () => {
         if (conn)
            conn.stop();
      };
   }, [code]);



   const startMatch = async () => {
      if (!connection) return;
      try {
         await connection.invoke("StartMatch");
      } catch (err) {
         console.error(err);
      }
   };


   if (phase === "game" && gameType) {
      const GameComponent = gameComponents[gameType];
      return <GameComponent
         gameId={code}
         playerId={user.id}
         connection={connection}
         onReturnToLobby={() => setPhase("lobby")}
      />;
   }

   return (
      <div>
         <h2>Lobby {code}</h2>
         <p>Your name is: {user.name}</p>
         <p>{message}</p>


         <button onClick={() => startMatch()}>Start Match</button>

         <p>Round {currentRound}/{totalRounds}</p>
         <h3>Players in Lobby:</h3>
         <ul>
            {players.map((player, idx) => (
               <li key={idx}>
                  {player.name} - Wins: {player.wins ?? 0}
               </li>
            ))}
         </ul>
      </div>
   );
}

export default LobbyPage;