import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { GetGameStats } from "./api/user";
import RetroButton from "./components/RetroButton";

function Profile() {
   const [username, setUsername] = useState("");
   const [authorized, setAuthorized] = useState(false); 
   const [loading, setLoading] = useState(true);  
   const [gameStats, setGameStats] = useState({});
   const navigate = useNavigate();


   useEffect(() => {
      const fetchStats = async () => {
         const token = localStorage.getItem("userToken");
         
         if (!token) {
            alert("Please login.");
            navigate("/");
            return;
         }
         try {
            const response = await GetGameStats(token);
            if(response.ok) {
               const stats = await response.json();
               setAuthorized(true);
               setGameStats(stats);
               setUsername(stats.name || "");
            }
            else {
               setAuthorized(false);
            }
         } catch (err) {
            setAuthorized(false);
            console.log(err);
         } finally {
            setLoading(false);
         }
      };

      fetchStats();
   }, [navigate]);

   const formatGameType = (str) => {
      const withSpaces = str.replace(/([A-Z])/g, ' $1');
      return withSpaces.charAt(0).toUpperCase() + withSpaces.slice(1);
   };
   
   const getWinRate = (wins, played) => (played > 0 ? ((wins / played) * 100).toFixed(1) + "%" : "0%");
   
   if (loading) {
      return (
         <div className="h-screen flex items-center justify-center text-2xl">
            Loading profile…
         </div>
	   );
   }

   const gameTypes = Object.keys(gameStats)
      .filter(key => key.endsWith("Wins") && !key.toLowerCase().startsWith("total"))
      .map(key => key.replace("Wins", ""))

   return (
      <div style={{ display: "flex", flexDirection: "column", fontSize: "24px", padding: "10px", justifyItems: "center", justifyContent: "center", alignItems: "center"}}>
         <h1>Profile page</h1>
         { !authorized ? (
            <div style={{ justifyItems: "center"}}>
                  <h2>You must be registered to view this page.</h2>
                  <div style={{ marginTop: "16px"}}>
                     <RetroButton onClick={() => navigate("/home")} bg="#2aaac4ff" w={350} h={40}>Go to Home</RetroButton>
                  </div>
            </div>
         ) : (
            <div>
               <h3 style={{ marginBottom: "16px", fontSize: "28px" }}>Welcome, {username}!</h3>
               <RetroButton onClick={() => navigate("/home")} bg="#2aaac4ff" w={350} h={40}>Go to Home</RetroButton>

               <hr style={{ marginTop: "32px", marginBottom: "16px" }}/>

               <h3 style={{ fontSize: "28px" }}>Statistics</h3>
               <p>Total Games Played: {gameStats.totalGamesPlayed || 0}</p>
               <p>Total Games Won: {gameStats.totalWins || 0}</p>
               <p>Win Rate: {getWinRate(gameStats.totalWins, gameStats.totalGamesPlayed)}</p>

               {gameTypes.map(gt => (
                  <div key={gt}>
                     <hr style={{ marginBlock: "16px" }}/>
                     <h4 style={{ fontSize: "28px" }}>{formatGameType(gt)}</h4>
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