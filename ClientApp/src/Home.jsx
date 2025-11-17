import { useNavigate } from "react-router-dom";
import { useState } from "react";
import { CreateLobby, CanJoinLobby } from "./api/lobby";
import { CanJoinQueue } from "./api/queue";

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
   }
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
      <div style={{ padding: "20px" }}>
         <h1>Home Page</h1>

         <div style={{ marginBottom: "30px" }}>
            <button onClick={handleQueueJoin} className="normal-button">Queue</button>
         </div>

         <hr />

         <div style={{ marginBottom: "30px" }}>
            <h2>Create New Lobby</h2>

            <div style={{ marginBottom: "10px" }}>
               <label>
                  Number of Players:
                  <input
                     type="number"
                     min="2"
                     max="10"
                     value={numberOfPlayers}
                     onChange={(e) => setNumberOfPlayers(parseInt(e.target.value) || 2)}
                     style={{ marginLeft: "10px", width: "60px" }}
                  />
               </label>
            </div>

            <div style={{ marginBottom: "10px" }}>
               <label>
                  Number of Rounds:
                  <input
                     type="number"
                     min="1"
                     max="5"
                     value={numberOfRounds}
                     onChange={(e) => {
                        const rounds = parseInt(e.target.value) || 1;
                        setNumberOfRounds(rounds);
                        if (!randomGames) {
                           const currentGames = gamesInput.split(",").map(g => g.trim()).filter(g => g);
                           if (currentGames.length > rounds) {
                              setGamesInput(currentGames.slice(0, rounds).join(","));
                           }
                        }
                     }}
                     style={{ marginLeft: "10px", width: "60px" }}
                  />
               </label>
            </div>

            <div style={{ marginBottom: "10px" }}>
               <label>
                  <input
                     type="checkbox"
                     checked={randomGames}
                     onChange={(e) => setRandomGames(e.target.checked)}
                     style={{ marginRight: "5px" }}
                  />
                  Random Games
               </label>
            </div>

            {!randomGames && (
               <div style={{ marginBottom: "10px" }}>
                  <label style={{ display: "block", marginBottom: "5px" }}>
                     Select Games for Each Round:
                  </label>
                  {Array.from({ length: numberOfRounds }).map((_, index) => {
                     const currentGames = gamesInput.split(",").map(g => g.trim());
                     const selectedGame = currentGames[index] || "TicTacToe";

                     return (
                        <div key={index} style={{ marginBottom: "5px" }}>
                           <label style={{ marginRight: "10px" }}>
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
                              style={{ padding: "5px", width: "200px" }}
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

            <button onClick={handleCreateLobby} className="normal-button">Create Lobby</button>
         </div>

         <hr />

         <div>
            <h2>Join Existing Lobby</h2>
            <input
               className="input-field"
               type="text"
               inputMode="numeric"
               placeholder="Lobby Code"
               value={lobbyID}
               onChange={e => setlobbyID(e.target.value.replace(/[^0-9]/g, ""))}
               style={{ marginRight: "10px" }}
            />
            <button onClick={handleLobbyJoin} className="normal-button">Join Lobby</button>
         </div>
      </div>
   );
}

export default Home;