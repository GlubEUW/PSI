import { useState } from "react";
import { useNavigate } from "react-router-dom";

function Start() {
   const [username, setUsername] = useState("");
   const navigate = useNavigate();

   const handleGuestLogin= () => {
      if(!username.trim()) {
         alert("Please enter a username.");
         return;
      }
      console.log("Guest username:", username);
      navigate("/home");
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