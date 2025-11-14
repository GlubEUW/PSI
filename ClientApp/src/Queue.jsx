import { useNavigate } from "react-router-dom";

function Queue() {
   const navigate = useNavigate();

   const goHome = () => {
      navigate("/home");
   };

   return (
      <div>
         <h1>Queue Page</h1>
         <p>This is the queue page.</p>
         <button onClick={goHome} className="normal-button">Back to Home</button>
      </div>
   );
}

export default Queue;