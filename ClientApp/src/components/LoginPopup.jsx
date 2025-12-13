import { useState, useEffect } from 'react';
import { Popup, Input, Button } from 'pixel-retroui';
import RetroButton from './RetroButton';

const LoginPopup = ({ handleSubmit, isPopupOpen, closePopup}) => {
   const [username, setUsername] = useState("");
   const [password, setPassword] = useState("");

   useEffect(() => {
      if (!isPopupOpen) {
         setUsername("");
         setPassword("");
      }
   }, [isPopupOpen]);

   const onSubmit = (e) => {
      e.preventDefault();
      handleSubmit(username, password);
   };

   return (
      <Popup
         bg="#cc7e00ff"
         baseBg="#995e00ff"
         isOpen={isPopupOpen}
         onClose={closePopup}
         className="text-center"
      >
         <h1 className="text-3xl mb-4" style={{ color: "#004eccff" }}>Welcome!</h1>
         <p className="mb-4" style={{ color: "#004eccff" }}>Please login to continue.</p>

         <form onSubmit={onSubmit} className=" flex flex-col gap-4 items-center">
            <Input 
               bg="#f2f2f2" 
               type="text"
               placeholder="Username" 
               value={username}
               onChange={(e) => setUsername(e.target.value)}
               required
               autoComplete="username"
            />
            <Input
               bg="#f2f2f2"
               type="password"
               placeholder="Password"
               value={password}
               onChange={(e) => setPassword(e.target.value)}
               required
               autoComplete="current-password"
            />

            <RetroButton type="submit" bg="#004eccff" borderColor="#000" textColor="#fff" w={120} h={34} font={16}>
               Login
            </RetroButton>
         </form>
      </Popup>
   );
}

export default LoginPopup;