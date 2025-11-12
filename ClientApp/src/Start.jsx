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

   const handleLogin = async () => {};

   const handleRegister = async () => {};

   return (
      <div>
         <h1>Main page</h1>
         <div style={{ marginBottom: "10px" }}>
            <input
               className="input-field"
               type="text"
               placeholder="Enter guest username"
               value={username}
               onChange={(e) => setUsername(e.target.value)}
            />
         </div>
         <div style={{ marginBottom: "10px" }}>
            <button onClick={handleGuestLogin} className="normal-button">Continue as Guest</button>
         </div>
         <div style={{ marginBottom: "10px" }}>
            <button onClick={handleLogin} className="normal-button"> Login</button>
         </div>
         <div style={{ marginBottom: "10px" }}>
            <button onClick={handleRegister} className="normal-button"> Register</button>
         </div>
      </div>
   );
}

export default Start;