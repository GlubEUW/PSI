import { useNavigate } from "react-router-dom";

function Home() {
  const navigate = useNavigate();
  
  const goToQueue = () => {
    navigate("/queue");
  };

  return (
    <div>
      <h1>Home page</h1>
      <button onClick={goToQueue} className="linkButton">Queue</button>
    </div>
  );
}

export default Home;