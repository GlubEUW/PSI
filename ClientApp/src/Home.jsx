import { useNavigate } from "react-router-dom";
import { useState } from "react";
import { CreateLobby, CanJoinLobby } from "./api/lobby";
import { CanJoinQueue } from "./api/queue";
import RetroButton from "./components/RetroButton";
import Dropdown from "./components/DropDown";
import { Input } from "pixel-retroui";

function Home() {
   const navigate = useNavigate();
   const [lobbyID, setlobbyID] = useState("");
   const [numberOfPlayers, setNumberOfPlayers] = useState(2);
   const [numberOfRounds, setNumberOfRounds] = useState(1);
   const [randomGames, setRandomGames] = useState(false);
   const [gamesInput, setGamesInput] = useState("TicTacToe");

   const handleQueueJoin = async () => {
      const token = localStorage.getItem("userToken");

      if (!token) {
         alert("You must be logged in to access the queue.");
         navigate("/");
         return;
      }

      try {
         const response = await CanJoinQueue(token);
         if (!response.ok) {
            console.log("Status: ", response.status);
            alert("Failed to join queue: " + "Status " + response.status);
            return;
         }
         console.log("Successfully joined queue.");
         alert("Successfully joined queue.");
      } catch (err) {
         console.error(err);
         alert("Something went wrong. Check console.");
      }
      navigate("/queue");
   };

   const handleProfileNavigate = async () => {
      navigate("/profile");
   };

   const handleLeaderBoardNavigate = async () => {
      navigate("/leaderboard");
   };

   const handleLobbyJoin = async () => {
      const token = localStorage.getItem("userToken");

      if (!token) {
         alert("You must be logged in to access the lobby.");
         navigate("/");
         return;
      }

      if (!lobbyID) {
         alert("Please enter a valid lobby ID.");
         return;
      }

      try {
         const response = await CanJoinLobby(token, lobbyID);
         if (!response.ok) {
            alert(await response.json().then(data => data.message, "Unresolved error"));
            return;
         }
      } catch (err) {
         console.error("Error fetching lobby info:", err);
         alert("Something went wrong. Please try again.");
         return;
      }
      navigate(`/match/${lobbyID}`);
   };

   const handleCreateLobby = async () => {
      const token = localStorage.getItem("userToken");

      if (!token) {
         alert("You must be logged in to create a lobby.");
         navigate("/");
         return;
      }

      let gamesList = null;

      if (!randomGames) {
         const games = gamesInput
            .split(",")
            .map(g => g.trim())
            .filter(g => g.length > 0);

         while (games.length < numberOfRounds) {
            games.push("TicTacToe");
         }

         gamesList = games.slice(0, numberOfRounds);
      }

      try {
         const response = await CreateLobby(token, numberOfPlayers, numberOfRounds, randomGames, gamesList);
         if (!response.ok) {
            const error = await response.json();
            alert(`Failed to create lobby: ${error.message || "Unknown error"}`);
            return;
         }

         const data = await response.json();
         navigate(`/match/${data.code}`);

      } catch (err) {
         alert("Something went wrong. Please try again.");
      }
   };

   return (
      <div style={{ fontSize: "18px" }}>
         <h1>Home Page</h1>

         <div style={{ marginBlock: "16px" }}>
            <RetroButton onClick={handleProfileNavigate} bg="#ff9d00ff" w={350} h={40}>Profile</RetroButton>
         </div>

         <div style={{ marginBlock: "16px" }}>
            <RetroButton onClick={handleLeaderBoardNavigate} bg="#2aaac4ff" w={350} h={40}>Leaderboard</RetroButton>
         </div>
         {/* <div style={{ marginBottom: "30px" }}>
            <button onClick={handleQueueJoin} className="normal-button">Queue</button>
         </div> */}

         <hr style={{ marginBlock: "16px" }} />

         <div>
            <h2 style={{ fontSize: "24px" }}>Create New Lobby</h2>

            <div style={{ marginBottom: "16px", marginTop: "8px" }}>
               <label>
                  Number of Players:
                  <span style={{ marginLeft: "13px" }}>
                     <Dropdown
                        bg="#2aaac4ff"
                        options={[2, 3, 4, 5, 6, 7, 8, 9, 10]}
                        onSelect={(num) => setNumberOfPlayers(num)}
                     ></Dropdown>
                  </span>
               </label>
            </div>

            <div style={{ marginBottom: "8px" }}>
               <label>
                  Number of Rounds:
                  <span style={{ marginLeft: "20px" }}>
                     <Dropdown
                        bg="#2aaac4ff"
                        options={[1, 2, 3, 4, 5]}
                        onSelect={(num) => setNumberOfRounds(num)}
                     ></Dropdown>
                  </span>
               </label>
            </div>

            <div style={{ marginBottom: "8px" }}>
               <label>
                  <input
                     type="checkbox"
                     checked={randomGames}
                     onChange={(e) => setRandomGames(e.target.checked)}
                     style={{ marginRight: "8px" }}
                     className="w-4 h-4"
                  />
                  Random Games
               </label>
            </div>

            {!randomGames && (
               <div style={{ marginTop: "8px", marginBottom: "16px" }}>
                  <label style={{ display: "block", marginBottom: "4px" }}>
                     Select Games for Each Round:
                  </label>
                  {Array.from({ length: numberOfRounds }).map((_, index) => {
                     const currentGames = gamesInput.split(",").map(g => g.trim());
                     const selectedGame = currentGames[index] || "TicTacToe";

                     return (
                        <div key={index} style={{ marginBottom: "4px" }}>
                           <label style={{ marginRight: "8px" }}>
                              Round {index + 1}:
                           </label>
                           <select
                              value={selectedGame}
                              onChange={(e) => {
                                 const games = gamesInput.split(",").map(g => g.trim());
                                 games[index] = e.target.value;
                                 while (games.length < numberOfRounds) {
                                    games.push("TicTacToe");
                                 }
                                 setGamesInput(games.slice(0, numberOfRounds).join(","));
                              }}
                              style={{ padding: "4px", width: "256px" }}
                           >
                              <option value="TicTacToe">Tic Tac Toe</option>
                              <option value="RockPaperScissors">Rock Paper Scissors</option>
                              <option value="ConnectFour">Connect Four</option>
                           </select>
                        </div>
                     );
                  })}
               </div>
            )}

            <RetroButton onClick={handleCreateLobby} bg="#44b17bff" w={350} h={40}>Create Lobby</RetroButton>
         </div>

         <hr style={{ marginBlock: "16px" }} />

         <div>
            <h2 style={{ fontSize: "24px", marginBlock: "8px" }}>Join Existing Lobby</h2>
            <Input
               bg="#0e081d"
               borderColor="#fff"
               textColor="#fff"
               type="text"
               inputMode="numeric"
               placeholder="Lobby Code"
               value={lobbyID}
               onChange={e => setlobbyID(e.target.value.replace(/[^0-9]/g, ""))}
               style={{ marginBottom: "16px" }}
            />
            <RetroButton onClick={handleLobbyJoin} bg="#2aaac4ff" w={350} h={40}>Join Lobby</RetroButton>
         </div>
      </div>
   );
}

export default Home;