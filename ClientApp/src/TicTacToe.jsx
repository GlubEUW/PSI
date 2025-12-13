import { useState, useEffect } from "react";

function TicTacToe({ gameId, playerId, connection, onReturnToLobby }) {
   const [board, setBoard] = useState([
      [0, 0, 0],
      [0, 0, 0],
      [0, 0, 0]
   ]);
   const [playerTurn, setPlayerTurn] = useState(null);
   const [winner, setWinner] = useState(null);

   useEffect(() => {
      if (!connection) {
         console.error("No connection provided.");
         return;
      }

      const handleGameUpdate = (game) => {
         console.log("Game update received:", game);
         if (game.board) {
            setBoard(game.board);
            setPlayerTurn(game.playerTurn);
            setWinner(game.winner);
         }
      };

      connection.on("GameUpdate", handleGameUpdate);

      connection.invoke("GetGameState", gameId)
         .then(state => {
            if (state) {
               setBoard(state.board);
               setPlayerTurn(state.playerTurn);
               setWinner(state.winner);
            }
         })
         .catch(err => console.error("Failed to get game state:", err));

      return () => {
         connection.off("GameUpdate", handleGameUpdate);
      };
   }, [connection, gameId]);

   useEffect(() => {
      if (!winner) return;

      const timer = setTimeout(() => {
         returnToLobby();
      }, 3000);

      return () => clearTimeout(timer);
   }, [winner]);

   const handleClick = (row, col) => {
      console.log(`Clicked cell [${row}][${col}], current value:`, board[row][col]);

      if (!connection || board[row][col] !== 0 || winner) {
         console.log("Move blocked:", {
            hasConnection: !!connection,
            cellValue: board[row][col],
            winner
         });
         return;
      }

      console.log("Sending move:", { PlayerId: playerId, X: row, Y: col });

      connection.invoke("MakeMove", { PlayerId: playerId, X: row, Y: col })
         .catch(err => console.error("Move failed:", err));
   };
   const returnToLobby = () => {
      if (connection)
         connection.invoke("EndGame", gameId)
            .catch(err => console.error("Failed to end game:", err));

      onReturnToLobby();
   };

   return (
      <div style={{ display: "flex", flexDirection: "column", alignItems: "center", padding: "10px", fontSize: "32px"}}>
         <h2>Tic Tac Toe</h2>
         {winner ? 
            (<div style={{ marginBottom: "20px" }}>
               <h3 style={{ color: "#54ff11ff" }}>Winner: {winner}</h3>
               <p>You will return to the lobby shortly.</p>
            </div>
         ) : (
            <h3>Current turn: {playerTurn}</h3>
         )}
         <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 80px)", gap: "5px", marginTop: "20px"}}>
            {board.map((row, i) =>
               row.map((cell, j) => (
                  <div
                     key={`${i}-${j}`}
                     onClick={() => handleClick(i, j)}
                     style={{
                        width: "80px",
                        height: "80px",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        fontSize: "32px",
                        border: "1px solid black",
                        cursor: cell || winner ? "default" : "pointer",
                        backgroundColor: cell ? "#eee" : "#fff",
                        color: cell === 1 ? "blue" : cell === 2 ? "red" : "black"
                     }}
                  >
                     {cell === 1 ? "X" : cell === 2 ? "O" : ""}
                  </div>
               ))
            )}
         </div>
      </div>
   );
}

export default TicTacToe;