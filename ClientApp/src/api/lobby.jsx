export async function CreateLobby(token, numberOfPlayers, numberOfRounds, randomGames, gamesList) {
   const response = await fetch("http://localhost:5243/api/lobby/create", {
      method: "POST",
      headers: {
         "Authorization": "Bearer " + token,
         "Content-Type": "application/json"
      },
      body: JSON.stringify({
         numberOfPlayers: numberOfPlayers,
         numberOfRounds: numberOfRounds,
         randomGames: randomGames,
         gamesList: gamesList
      })
   });
   return response;
}

export async function GetLobbyInfo(token, code) {
   const response = await fetch(`http://localhost:5243/api/lobby/${code}`, {
      method: "GET",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      }
   });
   return response;
}

export async function CanJoinLobby(token, code) {
   const response = await fetch(`http://localhost:5243/api/lobby/${code}/canjoin`, {
      method: "POST",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      }
   });
   return response;
}

