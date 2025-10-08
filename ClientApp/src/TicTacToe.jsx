import { useState, useEffect } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";

function TicTacToe({ gameID, isPlayerX }) {
   const [connection, setConnection] = useState(null);
   const [board, setBoard] = useState([
      [0, 0, 0],
      [0, 0, 0],
      [0, 0, 0]
   ]);
   const [isXTurn, setIsXTurn] = useState(true);
   const [winner, setWinner] = useState(null);


   useEffect(() => {

      if (!gameID) {
         console.error("No game ID provided.");
         alert("No game ID provided.");
         return;
      }

      const gameConn = new HubConnectionBuilder()
         .withUrl("http://localhost:5243/tictactoeHub")
         .withAutomaticReconnect()
         .build();

      gameConn.on("GameUpdate", (game) => {
         if (game?.Board) setBoard(game.Board); 
         if (typeof game?.IsXTurn === "boolean") setIsXTurn(game.IsXTurn);
         if (game?.Winner) setWinner(game.Winner);
      });

      gameConn.start()
         .then(() => {
            gameConn.invoke("StartGame", gameID, isPlayerX);
         })
         .catch(err => console.error('Failed to connect to game hub:', err));

      setConnection(gameConn);

      return () => {
         if (gameConn) gameConn.stop();
      };
   }, [gameID, isPlayerX]);

   const handleClick = (x, y) => {
      if (!connection || board[x][y] || winner) return;

      connection.invoke("MakeMove", gameID, x, y, isPlayerX)
         .catch(err => console.error("Move failed:", err));
   };

   return (
      <div>
         <h2>Tic Tac Toe</h2>
         {winner ? <h3>Winner: {winner}</h3> : <h3>Current turn: {isXTurn ? 'X' : 'O'}</h3>}
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
                        backgroundColor: cell ? "#eee" : "#fff"
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