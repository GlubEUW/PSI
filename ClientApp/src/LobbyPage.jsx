import { useState, useEffect, useRef } from "react";
import { GetUser } from "./api/user";
import { useParams, useNavigate, Outlet } from "react-router-dom";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { Card } from "pixel-retroui";
import RetroButton from "./components/RetroButton";

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
   const [tournamentStarted, setTournamentStarted] = useState(false);

   const connectedRef = useRef(false);

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

         const response = await GetUser(token);
         if (!response.ok) {
            setMessage("Failed to fetch user info. Redirecting...");
            setTimeout(() => navigate("/home"), 3000);
            return;
         }

         const userData = await response.json();
         setUser(userData);

         conn = new HubConnectionBuilder()
            .withUrl(`http://localhost:5243/TournamentHub?code=${code}`, {
               accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

         conn.on("PlayersUpdated", async (roundInfo) => {
            try {
               if (roundInfo) {
                  setCurrentRound(roundInfo.currentRound);
                  setTotalRounds(roundInfo.totalRounds);
                  if (roundInfo.currentRound > 0) {
                     setTournamentStarted(true);
                  }
               }

               const playerInfo = await conn.invoke("GetPlayers", code);
               setPlayers(playerInfo);
            } catch {
               setPlayers([]);
            }
         });

         conn.on("NoPairing", (message) => {
            setMessage("You were not matched due to an uneven player count.");
         });

         conn.on("Error", (errorMessage) => {
            setMessage(errorMessage);
            setTimeout(() => setMessage(""), 3000);
         });

         conn.on("GameStarted", (data) => {
            console.log("Round started!", data);
            navigate("game", {
               state: {
                  gameType: data.gameType,
               },
               replace: false
            });
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
   }, [code, navigate, token]);

   const handleStart = async () => {
      if (!connection) return;
      try {
         if (!tournamentStarted) {
            await connection.invoke("StartTournament");
            setTournamentStarted(true);
            await connection.invoke("StartRound");
         } else {
            await connection.invoke("StartRound");
         }
      } catch (err) {
         console.error(err);
      }
   };

   const quitLobby = async () => {
      if (connection) {
         await connection.stop();
      }
      navigate("/home");
   }

   const outletContext = { connection, user, code };

   return (
      <>
         <Outlet context={outletContext} />
         {!window.location.pathname.includes('/game') && (
            <div style={{ display: "flex", flexDirection: "column", fontSize: "20px", justifyContent: "center", justifyItems: "center", alignContent: "center", alignItems: "center" }}>
               <div>
                  <Card
                     bg="#44b17bff"
                     textColor="black"
                     borderColor="#fff"
                     shadowColor="#000"
                     className="w-100 h-12 justify-center items-center"
                     style={{ display: "flex", marginBottom: "16px", fontSize: "36px" }}
                  >
                     Lobby {code}
                  </Card>
                  <p style={{ fontSize: "24px" }}>Your name is: {user.name}</p>
               </div>
               <p style={{ marginBottom: "8px" }}>{message}</p>
               <div>
                  <RetroButton onClick={handleStart} bg="#2aaac4ff" w={350} h={40}>
                     {tournamentStarted ? "Start Round" : "Start"}
                  </RetroButton>
                  <hr style={{ marginTop: "32px", marginBottom: "16px" }} />

                  <p>Round {currentRound}/{totalRounds}</p>
                  <h3>Players in Lobby:</h3>
                  <ul>
                     {players.map((player, idx) => (
                        <li key={idx}>
                           {player.name} - Wins: {player.wins ?? 0}
                        </li>
                     ))}
                  </ul>

                  <hr style={{ marginTop: "16px", marginBottom: "32px" }} />

                  <RetroButton onClick={() => quitLobby()} bg="#ff9d00ff" w={350} h={40}>Quit Lobby</RetroButton>
               </div>
            </div>
         )}
      </>
   );
}

export default LobbyPage;