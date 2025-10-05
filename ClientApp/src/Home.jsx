import { useNavigate } from "react-router-dom";
import { useState } from "react";

function Home() {
   const navigate = useNavigate();
   const [lobbyID, setlobbyID] = useState("");

   const handleQueueJoin = async () => {
      const token = localStorage.getItem("guestToken");

      if (!token) {
         alert("You must be logged in to access the queue.");
         return;
      }

      try {
         const response = await fetch("http://localhost:5243/api/queue", {
            method: "POST",
            headers: {
               "Authorization": "Bearer " + token,
               "Content-Type": "application/json"
            },
         });

         if (!response.ok) {
            console.log("Status: ", response.status);
            const errorText = await response.text();
            console.log("Error text: ", errorText)
            alert("Failed to join queue: " + (errorText || "Status " + response.status));
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

   const handleLobbyCreation = async () => {
      if (!lobbyID) {
         alert("Please enter a valid lobby ID.");
         return;
      }
      navigate(`/match/${lobbyID}`);
   }
   return (
      <div>
         <h1>Home page</h1>
         <button onClick={handleQueueJoin} className="linkButton">Queue</button>
         <div>
            <input
               type="number"
               placeholder="Lobby Code"
               value={lobbyID}
               onChange={(e) => setlobbyID(e.target.value)}
            />
            <button onClick={handleLobbyCreation} className="linkButton">Lobby</button>
         </div>
      </div>
   );
}

export default Home;