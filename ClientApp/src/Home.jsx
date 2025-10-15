import { useNavigate } from "react-router-dom";
import { useState } from "react";
import { JoinLobby } from "./api/lobby";

function Home() {
   const navigate = useNavigate();
   const [lobbyID, setlobbyID] = useState("");

   const handleQueueJoin = async () => {
      const token = localStorage.getItem("guestToken");

      if (!token) {
         alert("You must be logged in to access the queue.");
         navigate("/");
         return;
      }

      try {
         const response = await fetch("http://localhost:5243/api/queue", { // FIXME: Refactor to separate file in ./api/
            method: "POST",
            headers: {
               "Authorization": "Bearer " + token,
               "Content-Type": "application/json"
            },
         });

         if (!response.ok) {
            console.log("Status: ", response.status);
            alert("Failed to join queue: " + "Status " + response.status);
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
      const token = localStorage.getItem("guestToken");

      if (!token) {
         alert("You must be logged in to access the lobby.");
         navigate("/");
         return;
      }

      if (!lobbyID) {
         alert("Please enter a valid lobby ID.");
         return;
      }

      try {
         const lobbyInfoResponse = await JoinLobby(token, lobbyID);
         if (!lobbyInfoResponse.ok) {
            alert(await lobbyInfoResponse.json().then(data => data.message, "Unresolved error"));
            return;
         }
      } catch (err) {
         console.error("Error fetching lobby info:", err);
         alert("Something went wrong. Please try again.");
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
               type="text"
               inputMode="numeric"
               placeholder="Lobby Code"
               value={lobbyID}
               onChange={e => setlobbyID(e.target.value.replace(/[^0-9]/g, ""))}
            />
            <button onClick={handleLobbyCreation} className="linkButton">Lobby</button>
         </div>
      </div>
   );
}

export default Home;