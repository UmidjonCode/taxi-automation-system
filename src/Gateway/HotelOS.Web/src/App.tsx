import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import { Building2, Key, CalendarCheck, User } from 'lucide-react';
import LandingPage from './pages/LandingPage';
import SearchRoomsPage from './pages/SearchRoomsPage';
import LoginPage from './pages/LoginPage';
import MyReservationsPage from './pages/MyReservationsPage';
import './index.css';

function Navbar() {
  const navigate = useNavigate();
  const email = localStorage.getItem('guestEmail');

  const handleLogout = () => {
    localStorage.removeItem('guestEmail');
    navigate('/');
  };

  return (
    <nav className="navbar">
      <Link to="/" className="nav-logo">
        <Building2 size={28} color="#3b82f6" />
        HotelOS
      </Link>
      <div className="nav-links">
        <Link to="/rooms"><Key size={18} style={{display:'inline', marginRight:4}}/> Rooms</Link>
        {email ? (
          <>
            <Link to="/my-reservations"><CalendarCheck size={18} style={{display:'inline', marginRight:4}}/> My Reservations</Link>
            <a href="#" onClick={(e) => { e.preventDefault(); handleLogout(); }}>Logout ({email})</a>
          </>
        ) : (
          <Link to="/login"><User size={18} style={{display:'inline', marginRight:4}}/> Guest Login</Link>
        )}
      </div>
    </nav>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/rooms" element={<SearchRoomsPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/my-reservations" element={<MyReservationsPage />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
