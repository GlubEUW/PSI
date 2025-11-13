import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { PostUserLogin } from "./api/user";

function Login() {
   const [username, setUsername] = useState("");
   const [password, setPassword] = useState("");
   const navigate = useNavigate();

   const handleLogin = async () => {
      if(!username.trim()) {
         alert("Please enter a username.");
         return;
      }
      if(!password.trim()) {
         alert("Please enter a password.");
         return;
      }

      try {
         const response = await PostUserLogin(username, password);

         if(!response.ok) {
            const errorText = await response.text();
            alert("Login failed: " + errorText);
            return;
         }
         const token = await response.text();
         localStorage.setItem("userToken", token);
         console.log("Login successfull");
         navigate("/home");

      } catch (error) {
         console.error("Error during user registration: ", error);
         alert("Something went wrong. Check console.");
      }
   };

   return (
      <div>
         <h1>Login</h1>
         <form 
            onSubmit={(e) => {
               e.preventDefault();
               handleLogin();
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
                  autoComplete="current-password"
               />
            </div>
            <div>
               <button type="submit" className="normal-button">Login</button>
            </div>
         </form>
      </div>
   );
}

export default Login;