import { useEffect, useState } from "react";
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from "react-router-dom";

function Home() {
  const navigate = useNavigate();


  const goToQueue = () => {
    navigate("/queue");
  };

  return (
    <div>
      <h1>Main page</h1>
      <button onClick={goToQueue} className="linkButton">Queue</button>

      
    </div>
  );
}

function Queue() {
  return (
    <div>
      <h1>Queue Page</h1>
      <p>This is the queue page.</p>
      <Link to="/">Back to Home</Link>
    </div>
  );
}

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/queue" element={<Queue />} />
      </Routes>
    </Router>
  );
}

export default App;