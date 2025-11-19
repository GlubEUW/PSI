import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import "./App.css";
import Start from "./Start";
import Login from "./Login";
import Register from "./Register";
import Profile from "./Profile";
import Home from "./Home";
import Queue from "./Queue";
import LobbyPage from "./LobbyPage";
import GameContainer from "./GameContainer";

function App() {
   return (
      <Router>
         <Routes>
            <Route path="/" element={<Start />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/profile" element={<Profile />} />
            <Route path="/home" element={<Home />} />
            <Route path="/queue" element={<Queue />} />
            <Route path="/match/:code" element={<LobbyPage />}>
               <Route path="game" element={<GameContainer />} />
            </Route>
         </Routes>
      </Router>
   );
}

export default App;