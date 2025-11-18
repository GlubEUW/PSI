import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { GetUser } from "./api/user";

function Profile() {
   const [username, setUsername] = useState("");
   const navigate = useNavigate();


   useEffect(() => {
      const token = localStorage.getItem("userToken");
      if (token) {
         GetUser(token).then(async (response) => {
            if (response.ok) {
               const data = await response.json();
               setUsername(data.name);
            } else {
               navigate("/login");
            }
         });
      }
      else {
         navigate("/login");
      };
   }, [navigate]);
   return (
      <div>
         <h1>Profile page</h1>

         <h3>Welcome, {username}!</h3>
         <button onClick={() => navigate("/home")}>Go to Home</button>

         <h3>Statistics</h3>
         <p>Games Played: </p>
         <p>Games Won: </p>
         <p>Win Rate: </p>
      </div>
   );

}
export default Profile;