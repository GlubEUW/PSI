import { useState, useEffect } from "react";
import RetroButton from "./components/RetroButton";

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

   useEffect(() => {
      if (!game.result) return;

      const timer = setTimeout(() => {
         returnToLobby();
      }, 3000);

      return () => clearTimeout(timer);
   }, [game.result]);

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
      <div style={{ padding: "10px", fontSize: "32px" }}>
         <h2>Rock Paper Scissors</h2>
         {game.result ? (
            <div>
               <h3 style={{ color: "#54ff11ff" }}>{game.result}</h3>
               <p>You will return to the lobby shortly.</p>
            </div>
         ) : (
            <h3>Your choice: {myChoice || "None"}</h3>
         )}

         <div style={{ display: "flex", gap: "10px", marginTop: "20px", justifyContent: "center", justifyItems: "center", alignContent: "center", alignItems: "center"}}>
            {choices.map((c) => (
               <RetroButton
                  key={c}
                  onClick={() => makeMove(c)}
                  disabled={!canPlay}
                  className={canPlay ? "opacity-100" : "opacity-50"}
                  bg={myChoice === c ? "#48bb4cff" : "#6d6d6dff"}
                  textColor="#fff"
                  w={150}
                  h={50}
               >
                  {c}
               </RetroButton>
            ))}
         </div>
      </div>
   );
}

export default RockPaperScissors;