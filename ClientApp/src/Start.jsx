import { useState } from "react";
import { useNavigate } from "react-router-dom";

function Start() {
   const [username, setUsername] = useState("");
   const navigate = useNavigate();

   const handleGuestLogin= async () => {
      if(!username.trim()) {
         alert("Please enter a username.");
         return;
      }

      try {
         const response = await fetch("http://localhost:5243/api/auth/guestLogin", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name:username })
         });

         if(!response.ok) {
            const errorText = await response.text();
            alert("Login failed: " + errorText);
            return;
         }

         const token = await response.text();
         localStorage.setItem("guestToken", token);
         navigate("/home");
      } catch (error) {
         console.error("Error during guest login: ", error);
         alert("Something went wrong. Check console.");
      }
   };

   return (
      <div>
         <h1>Main page</h1>
         <input
            type="text"
            placeholder="Enter guest username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
         />
         <button onClick={handleGuestLogin}>Continue as Guest</button>
      </div>
   );
}

export default Start;