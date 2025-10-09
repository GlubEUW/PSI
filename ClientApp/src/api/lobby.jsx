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
