import { useState, useEffect } from "react";
import TicTacToe from "./TicTacToe.jsx";
import Rps from "./Rps.jsx";

const gameComponents = {
   TicTacToe: TicTacToe,
   RockPaperScissors: Rps
};

function TournamentPage({ code, playerName, connection }) {
   const [message, setMessage] = useState("");
   const [phase, setPhase] = useState("setup");
   const [gameType, setGameType] = useState("TicTacToe");
   const [tournamentStatus, setTournamentStatus] = useState(null);
   const [currentMatchData, setCurrentMatchData] = useState(null);
   const [leaderboard, setLeaderboard] = useState([]);

   useEffect(() => {
      if (!connection) return;



      return () => {
      };
   }, [connection]);

   const startTournament = async () => {
      if (!connection) return;
      try {
         await connection.invoke("StartTournament", code, gameType);
      } catch (err) {
         console.error("Failed to start tournament:", err);
         setMessage("Failed to start tournament.");
      }
   };

   connection.on("TournamentMatchStarted", (matchData) => {
      console.log("Tournament match started:", matchData);
      setCurrentMatchData(matchData);
      setPhase("match");
      setMessage(`Match: ${matchData.player1} vs ${matchData.player2}`);
   });

   const recordMatchResult = async (winner, isDraw = false) => {
      if (!connection || !currentMatchData) return;
      try {
         await connection.invoke(
            "RecordTournamentMatchResult",
            code,
            currentMatchData.gameId,
            winner,
            isDraw
         );
         setPhase("tournament");
      } catch (err) {
         console.error("Failed to record match result:", err);
      }
   };



   if (phase === "tournament") {
      return (
         <div>
            <h2>Tournament in Progress</h2>
            <p>{message}</p>

            {tournamentStatus && (
               <div>
                  <h3>Tournament Status</h3>
                  <p>Matches: {tournamentStatus.completedMatches} / {tournamentStatus.totalMatches}</p>
                  <p>Game Type: {tournamentStatus.gameType}</p>
               </div>
            )}

            {leaderboard.length > 0 && (
               <div>
                  <h3>Current Standings</h3>
                  <table>
                     <thead>
                        <tr>
                           <th>Rank</th>
                           <th>Player</th>
                           <th>Score</th>
                           <th>W</th>
                           <th>D</th>
                           <th>L</th>
                        </tr>
                     </thead>
                     <tbody>
                        {leaderboard.map((player, idx) => (
                           <tr key={idx}>
                              <td>{idx + 1}</td>
                              <td>{player.playerName}</td>
                              <td>{player.score}</td>
                              <td>{player.wins}</td>
                              <td>{player.draws}</td>
                              <td>{player.losses}</td>
                           </tr>
                        ))}
                     </tbody>
                  </table>
               </div>
            )}

            <p>Waiting for next match to start...</p>
         </div>
      );
   }
   if (phase === "game" && currentMatchData) {
      const GameComponent = gameComponents[tournamentStatus.gameType];
      return <GameComponent
         gameId={currentMatchData.gameId}
         playerName={playerName}
         connection={connection}
         onReturnToLobby={() => recordMatchResult(currentMatchData.player1 === playerName ? currentMatchData.player2 : currentMatchData.player1)}
      />;
   }

}

export default TournamentPage;