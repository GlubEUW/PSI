import { Button } from 'pixel-retroui';
import "./RetroButton.css";

const RetroButton = ({ bg, ...props }) => (
   <Button
      className="w-full py-1 retro-button"
      style={{ "--bg": bg, fontSize: "24px" }}
      bg={bg}
      textColor="#000000ff"
      borderColor="#fff"
      shadow="#000000ff"
      rounded={false}
      {...props}
   />
);

export default RetroButton;