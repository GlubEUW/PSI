import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { GetGameStats } from "./api/user";
import RetroButton from "./components/RetroButton";

function Profile() {
   const [username, setUsername] = useState("");
   const [authorized, setAuthorized] = useState(false);   
   const [gameStats, setGameStats] = useState({});
   const navigate = useNavigate();


   useEffect(() => {
      const fetchStats = async () => {
         const token = localStorage.getItem("userToken");
         
         if (token) {
            const response = await GetGameStats(token);
            if(response.ok) {
               const stats = await response.json();
               setAuthorized(true);
               setGameStats(stats);
               setUsername(stats.name || "");
               console.log(gameStats);
            }
            else {
               setAuthorized(false);
               console.log("Error status: " + response.status);
            }
         }
         else {
            alert("Please login.");
            navigate("/login");
            return;
         }
      };

      fetchStats();
   }, [navigate]);

   const getWinRate = (wins, played) => (played > 0 ? ((wins / played) * 100).toFixed(1) + "%" : "0%");

   const gameTypes = Object.keys(gameStats)
      .filter(key => key.endsWith("Wins") && !key.toLowerCase().startsWith("total"))
      .map(key => key.replace("Wins", ""))

   return (
      <div>
         <h1>Profile page</h1>
         { !authorized ? (
            <div>
               <h2>You must be registered to view this page.</h2>
               <RetroButton onClick={() => navigate("/home")} bg="#ff9d00ff" w={350} h={40}>Go to Home</RetroButton>
            </div>
         ) : (
            <div>
               <h3>Welcome, {username}!</h3>
               <RetroButton onClick={() => navigate("/home")} bg="#ff9d00ff" w={350} h={40}>Go to Home</RetroButton>

               <h3>Statistics</h3>
               <p>Total Games Played: {gameStats.totalGamesPlayed || 0}</p>
               <p>Total Games Won: {gameStats.totalWins || 0}</p>
               <p>Win Rate: {getWinRate(gameStats.totalWins, gameStats.totalGamesPlayed)}</p>

               {gameTypes.map(gt => (
                  <div key={gt}>
                     <h4>{gt.charAt(0).toUpperCase() + gt.slice(1)}</h4>
                     <p>Games Played: {gameStats[`${gt}GamesPlayed`] || 0}</p>
                     <p>Games Won: {gameStats[`${gt}Wins`] || 0}</p>
                     <p>Win Rate: {getWinRate(gameStats[`${gt}Wins`], gameStats[`${gt}GamesPlayed`])}</p>
                  </div>
               ))}
            </div>
         )}
      </div>
   );
   {/* TODO: Let user change nickname and password */}
   {/* TODO: Add profile photo */}
}
export default Profile;