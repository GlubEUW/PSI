import { useState, useEffect } from "react";

const choices = ["Rock", "Paper", "Scissors"];

function RockPaperScissors({ gameId, playerId, connection, onReturnToLobby }) {
   const [game, setGame] = useState({ players: {}, result: null });
   const [myChoice, setMyChoice] = useState(null);

   useEffect(() => {
      if (!connection) 
         return;

      const handleGameUpdate = (updatedGame) => {
         setGame({
            players: updatedGame.players || {},
            result: updatedGame.result || null
         });
      };

      connection.on("GameUpdate", handleGameUpdate);

      connection.invoke("GetGameState", gameId)
         .then(state => {
            if (state) {
               setGame({
                  players: state.players || {},
                  result: state.result || null
               });
            }
         })
         .catch(err => console.error("Failed to get RockPaperScissors game state:", err));

      return () => connection.off("GameUpdate", handleGameUpdate);
   }, [connection, gameId]);

   const makeMove = (selectedChoice) => {
      if (!connection || myChoice !== null || game.result !== null) 
         return;

      setMyChoice(selectedChoice);
      const choiceValue = { "Rock": 1, "Paper": 2, "Scissors": 3 }[selectedChoice];

      connection.invoke("MakeMove", { PlayerId: playerId, Choice: choiceValue })
         .catch(err => console.error("Move failed:", err));
   };

   const returnToLobby = () => {
      if (connection)
         connection.invoke("EndGame", gameId).catch(console.error);

      onReturnToLobby();
   };

   const hasNotChosen = (myChoice === null);
   const gameNotOver = (game.result === null);
   const canPlay = hasNotChosen && gameNotOver;

   return (
      <div style={{ padding: "20px" }}>
         <h2>Rock Paper Scissors</h2>
         {game.result ? (
            <div>
               <h3 style={{ color: "green" }}>{game.result}</h3>
               <button onClick={returnToLobby} className="normal-button">Back To Lobby</button>
            </div>
         ) : (
            <h3>Your choice: {myChoice || "None"}</h3>
         )}

         <div style={{ display: "flex", gap: "10px", marginTop: "20px" }}>
            {choices.map((c) => (
               <button
                  key={c}
                  onClick={() => makeMove(c)}
                  disabled={!canPlay}
                  style={{
                     padding: "15px 30px",
                     fontSize: "18px",
                     cursor: canPlay ? "pointer" : "not-allowed",
                     opacity: canPlay ? 1 : 0.5,
                     backgroundColor: myChoice === c ? "#4CAF50" : "#fff",
                     color: myChoice === c ? "#fff" : "#000",
                     border: "2px solid #333",
                     borderRadius: "8px"
                  }}
               >
                  {c}
               </button>
            ))}
         </div>
      </div>
   );
}

export default RockPaperScissors;