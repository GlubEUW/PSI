import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { PostUserRegistration } from "./api/user";

function Register() {
   const [username, setUsername] = useState("");
   const [password, setPassword] = useState("");
   const navigate = useNavigate();

   const handleRegistration = async () => {
      if(!username.trim()) {
         alert("Please enter a username.");
         return;
      }
      if(!password.trim()) {
         alert("Please enter a password.");
         return;
      }

      try {
         const response = await PostUserRegistration(username, password);

         if(!response.ok) {
            const errorText = await response.text();
            console.error("Registration failed: ", errorText);
            return;
         }

         alert("Registration successfull!");
         navigate("/login");

      } catch (error) {
         console.error("Error during user registration: ", error);
      }
   };

   return (
      <div>
         <h1>Register</h1>
         <form 
            onSubmit={(e) => {
               e.preventDefault();
               handleRegistration();
            }}
         >
            <div style={{ marginBottom: "10px" }}>
               <input
                  className="input-field"
                  type="text"
                  placeholder="Enter username"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  autoComplete="username"
               />
            </div>
            <div style={{ marginBottom: "10px" }}>
               <input
                  className="input-field"
                  type="password"
                  placeholder="Enter password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="new-password"
               />
            </div>
            <div>
               <button type="submit" className="normal-button">Register</button>
            </div>
         </form>
      </div>
   );
}

export default Register;