import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { PutGuestUser } from "./api/user";

function Start() {
   const [username, setUsername] = useState("");
   const navigate = useNavigate();

   const handleGuestLogin = async () => {
      if (!username.trim()) {
         alert("Please enter a username.");
         return;
      }

      try {
         const response = await PutGuestUser(username);

         if (!response.ok) {
            const errorText = await response.text();
            alert("Login failed: " + errorText);
            return;
         }

         const token = await response.text();
         localStorage.setItem("userToken", token);
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