import { Link } from "react-router-dom";

function Queue() {
   return (
      <div>
         <h1>Queue Page</h1>
         <p>This is the queue page.</p>
         <Link to="/home">Back to Home</Link>
      </div>
   );
}

export default Queue;