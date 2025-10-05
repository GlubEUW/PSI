import { useEffect, useState } from "react";
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from "react-router-dom";
import "./App.css";
import Start from "./Start";
import Home from "./Home";
import Queue from "./Queue";
import LobbyPage from "./LobbyPage";

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
