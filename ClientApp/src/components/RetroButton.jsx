import { Button } from 'pixel-retroui';
import "./RetroButton.css";

const RetroButton = ({ bg="#fff", w=460, h=54, font=24, ...props }) => (
   <Button
      className="w-full py-1 retro-button"
      style={{ 
         "--bg": bg, 
         fontSize: font, 
         width: w, 
         height: h, 
         display: "flex", 
         justifyContent: "center", 
         alignItems: "center",
         textAlign: "center" 
      }}
      bg={bg}
      textColor="#000000ff"
      borderColor="#fff"
      shadow="#000000ff"
      // rounded={false}
      {...props}
   />
);

export default RetroButton;