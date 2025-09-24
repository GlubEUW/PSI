import { useEffect, useState } from "react";
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from "react-router-dom";
import "./App.css";
import Start from "./Start";
import Home from "./Home";
import Queue from "./Queue";
import LobbyPage from "./LobbyPage";
function Home() {
  const navigate = useNavigate();


  const goToQueue = () => {
    // FIXME: enter the player into queue logic here
    navigate("/queue");
  };
  const goToLobbyPage = () => {
    const randomCode = Math.random().toString(36).substring(2, 8).toUpperCase();
    navigate(`/match/${randomCode}`);
  };


  return (
    <div>
      <h1>Main page</h1>
      <button onClick={goToQueue} className="linkButton">Queue</button>
      <button onClick={goToLobbyPage} className="linkButton">Create Match Lobby</button>

    </div>
  );
}


function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Start />} />
        <Route path="/home" element={<Home />} />
        <Route path="/queue" element={<Queue />} />
        <Route path="/match/:code" element={<LobbyPage />} />
      </Routes>
    </Router>
  );
}

export default App;
