import { useState, useEffect } from "react";
import RetroButton from "./components/RetroButton";

function ConnectFour({ player, connection, onReturnToLobby }) {
   const [board, setBoard] = useState([]);
   const [playerTurn, setPlayerTurn] = useState(null);
   const [winner, setWinner] = useState(null);
   const [hoveredColumn, setHoveredColumn] = useState(null);
   const [gameOver, setGameOver] = useState(false);

   useEffect(() => {
      if (!connection) {
         console.error("No connection provided.");
         return;
      }

      const handleGameUpdate = (game) => {
         console.log("Connect Four update received:", game);
         if (game.board) {
            setBoard(game.board);
            setPlayerTurn(game.playerTurn);
            setWinner(game.winner);
         }
      };

      connection.on("GameUpdate", handleGameUpdate);

      connection.invoke("GetGameState")
         .then(state => {
            console.log("Connect Four initial state:", state);
            if (state) {
               setBoard(state.board);
               setPlayerTurn(state.playerTurn);
               setWinner(state.winner);
               setGameOver(state.GameOver)
            }
         })
         .catch(err => console.error("Failed to get Connect Four game state:", err));

      return () => {
         connection.off("GameUpdate", handleGameUpdate);
      };
   }, [connection]);

   useEffect(() => {
      if (!winner) return;

      const timer = setTimeout(() => {
         returnToLobby();
      }, 3000);

      return () => clearTimeout(timer);
   }, [winner]);

   const handleColumnClick = (column) => {
      console.log(`Clicked column ${column}`);

      if (!connection || winner) {
         console.log("Move blocked:", {
            hasConnection: !!connection,
            winner
         });
         return;
      }

      if (board.length > 0 && board[0][column] !== 0) {
         console.log("Column is full");
         return;
      }

      console.log("Sending move:", { Column: column });

      connection.invoke("MakeMove", { Column: column })
         .catch(err => console.error("Move failed:", err));
   };

   const returnToLobby = () => {
      onReturnToLobby();
   };

   const getDiscColor = (cell) => {
      if (cell === 1) return "#ef4444";
      if (cell === 2) return "#fbbf24";
      return "white";
   };

   const canDropInColumn = (column) => {
      if (!board || board.length === 0) return false;
      return board[0][column] === 0;
   };

   if (!board || board.length === 0) {
      return (
         <div style={{ padding: "20px", textAlign: "center", fontSize: "32px" }}>
            <h2>Connect Four</h2>
            <p>Loading game board...</p>
         </div>
      );
   }

   return (
      <div style={{ display: "flex", flexDirection: "column", alignItems: "center", padding: "10px", fontSize: "32px" }}>
         <h2>Connect Four</h2>

         {gameOver ? (
            winner ? (
               <div style={{ marginBottom: "20px" }} >
                  <h3 style={{ color: "green" }}>Winner: {winner}!</h3>
                  <p>You will return to the lobby shortly.</p>
               </div>)
               : (<h3 style={{ color: "orange" }}>It's a draw!</h3>
               )

         ) : (
            <h3>Current turn: {playerTurn.name}</h3>
         )}

         <div
            style={{
               display: "inline-block",
               backgroundColor: "#2563eb",
               padding: "20px",
               borderRadius: "12px",
               boxShadow: "0 10px 30px rgba(0,0,0,0.3)",
               marginBlock: "20px"
            }}
         >
            <div
               style={{
                  display: "grid",
                  gridTemplateColumns: "repeat(7, 60px)",
                  gap: "8px"
               }}
            >
               {board.map((row, rowIndex) =>
                  row.map((cell, colIndex) => (
                     <div
                        key={`${rowIndex}-${colIndex}`}
                        style={{
                           width: "60px",
                           height: "60px",
                           borderRadius: "50%",
                           backgroundColor: getDiscColor(cell),
                           border: "3px solid #1e40af",
                           cursor:
                              !winner && rowIndex === 0 && canDropInColumn(colIndex)
                                 ? "pointer"
                                 : "default",
                           boxShadow: "inset 0 2px 4px rgba(0,0,0,0.2)",
                           transition: "transform 0.2s",
                           transform:
                              !winner &&
                                 rowIndex === 0 &&
                                 hoveredColumn === colIndex &&
                                 canDropInColumn(colIndex)
                                 ? "scale(1.1)"
                                 : "scale(1)",
                           outline:
                              !winner &&
                                 rowIndex === 0 &&
                                 hoveredColumn === colIndex &&
                                 canDropInColumn(colIndex)
                                 ? "4px solid white"
                                 : "none"
                        }}
                     />
                  ))
               )}
            </div>
         </div>

         <div style={{ display: "flex", gap: "0px", marginBottom: "20px" }}>
            {[0, 1, 2, 3, 4, 5, 6].map((col) => (
               <RetroButton
                  key={col}
                  onClick={() => handleColumnClick(col)}
                  disabled={winner || !canDropInColumn(col)}
                  className={winner || !canDropInColumn(col) ? "opacity-50" : "opacity-100"}
                  bg={winner || !canDropInColumn(col) ? "#9ca3af" : "#3b82f6"}
                  w={45}
                  h={35}
                  borderColor="#fff"
               >
                  ↓
               </RetroButton>
            ))}
         </div>
      </div >
   );
}

export default ConnectFour;