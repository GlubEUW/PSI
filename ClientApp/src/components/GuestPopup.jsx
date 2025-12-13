import { useState, useEffect } from 'react';
import { Popup, Input, Button } from 'pixel-retroui';
import RetroButton from './RetroButton';

const GuestPopup = ({ handleSubmit, isPopupOpen, closePopup}) => {
   const [username, setUsername] = useState("");
   
   useEffect(() => {
      if (!isPopupOpen) {
         setUsername("");
      }
   }, [isPopupOpen]);

   const onSubmit = (e) => {
      e.preventDefault();
      handleSubmit(username);
   };

   return (
      <Popup
         bg="#0dc1f2ff"
         baseBg="#0a9ac2ff"
         isOpen={isPopupOpen}
         onClose={closePopup}
         className="text-center"
      >
         <h1 className="text-3xl mb-4" style={{ color: "#f5653dff" }}>Welcome!</h1>
         <p className="mb-4" style={{ color: "#f5653dff" }}>Please enter a username to continue.</p>

         <form onSubmit={onSubmit} className=" flex flex-col gap-4 items-center">
            <Input 
               bg="#f2f2f2" 
               type="text"
               placeholder="Username" 
               value={username}
               onChange={(e) => setUsername(e.target.value)}
               required
            />

            <RetroButton type="submit" bg="#f5653dff" borderColor="#000" w={120} h={34} font={16}>
               Continue
            </RetroButton>
         </form>
      </Popup>
   );
}

export default GuestPopup;