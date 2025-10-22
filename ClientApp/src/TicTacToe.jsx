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

      // Listen for game updates
      const handleGameUpdate = (game) => {
         console.log("Game update received:", game);
         if (game.board) {
            setBoard(game.board);
            setPlayerTurn(game.playerTurn);
            setWinner(game.winner);
         }
      };

      connection.on("GameUpdate", handleGameUpdate);

      // Request initial game state
      connection.invoke("GetGameState", gameId)
         .then(state => {
            if (state) {
               setBoard(state.board);
               setPlayerTurn(state.playerTurn);
               setWinner(state.winner);
            }
         })
         .catch(err => console.error("Failed to get game state:", err));

      // Cleanup listener on unmount
      return () => {
         connection.off("GameUpdate", handleGameUpdate);
      };
   }, [connection, gameId]);

   const handleClick = (x, y) => {
      if (!connection || board[x][y] !== 0 || winner) return;

      connection.invoke("MakeMove", { PlayerId: playerId, X: x, Y: y })
         .catch(err => console.error("Move failed:", err));
   };

   const returnToLobby = () => {
      if (connection) {
         connection.invoke("EndGame", gameId)
            .catch(err => console.error("Failed to end game:", err));
      }
      onReturnToLobby();
   };

   return (
      <div>
         <h2>Tic Tac Toe</h2>
         {winner ? <h3>Winner: {winner}</h3> : <h3>Current turn: {playerTurn}</h3>}
         <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 60px)", gap: "5px" }}>
            {board.map((row, i) =>
               row.map((cell, j) => (
                  <div
                     key={`${i}-${j}`}
                     onClick={() => handleClick(i, j)}
                     style={{
                        width: "60px",
                        height: "60px",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        fontSize: "24px",
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

         {winner && (
            <div>
               <button onClick={returnToLobby} className="linkButton">Back To Lobby</button>
            </div>
         )}
      </div>
   );
}

export default TicTacToe;