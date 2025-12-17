import { useLocation, useNavigate, useOutletContext } from "react-router-dom";
import TicTacToe from "./TicTacToe.jsx";
import RockPaperScissors from "./RockPaperScissors.jsx";
import ConnectFour from "./ConnectFour.jsx";

const gameComponents = {
   TicTacToe: TicTacToe,
   RockPaperScissors: RockPaperScissors,
   ConnectFour: ConnectFour
};

function GameContainer() {
   const { state } = useLocation();
   const navigate = useNavigate();
   const { connection, user, code } = useOutletContext();

   console.log("GameContainer context:", { connection, user, code, state });

   if (!connection || !user || !code) {
      return <p>Loading game... (connection: {!!connection}, user: {!!user}, code: {code})</p>;
   }

   const gameType = state?.gameType;

   if (!gameType || !gameComponents[gameType]) {
      navigate("..", { replace: true });
      return null;
   }

   const GameComponent = gameComponents[gameType];

   const handleReturnToLobby = () => {
      navigate("..", { replace: true });
   };

   return (
      <GameComponent
         player={user}
         connection={connection}
         onReturnToLobby={handleReturnToLobby}
      />
   );
}

export default GameContainer;