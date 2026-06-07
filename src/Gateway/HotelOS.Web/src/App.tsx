import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import { Building2, Key, CalendarCheck, User, LogOut } from 'lucide-react';
import LandingPage from './pages/LandingPage';
import SearchRoomsPage from './pages/SearchRoomsPage';
import LoginPage from './pages/LoginPage';
import MyReservationsPage from './pages/MyReservationsPage';
import './index.css';

// Helper to get auth state from localStorage
export function getAuth() {
  const token = localStorage.getItem('token');
  const email = localStorage.getItem('email');
  const fullName = localStorage.getItem('fullName');
  const guestId = localStorage.getItem('guestId');
  const role = localStorage.getItem('role');
  return { token, email, fullName, guestId, role, isLoggedIn: !!token };
}

// Helper to make authenticated API calls
export async function authFetch(url: string, options: RequestInit = {}) {
  const { token } = getAuth();
  const headers: any = { 'Content-Type': 'application/json', ...options.headers };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return fetch(url, { ...options, headers });
}

function Navbar() {
  const navigate = useNavigate();
  const { isLoggedIn, fullName, email } = getAuth();

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('email');
    localStorage.removeItem('fullName');
    localStorage.removeItem('guestId');
    localStorage.removeItem('role');
    navigate('/');
    window.location.reload();
  };

  return (
    <nav className="navbar">
      <Link to="/" className="nav-logo">
        <Building2 size={28} color="#3b82f6" />
        HotelOS
      </Link>
      <div className="nav-links">
        <Link to="/rooms"><Key size={18} style={{display:'inline', marginRight:4}}/> Rooms</Link>
        {isLoggedIn ? (
          <>
            <Link to="/my-reservations"><CalendarCheck size={18} style={{display:'inline', marginRight:4}}/> My Reservations</Link>
            <a href="#" onClick={(e) => { e.preventDefault(); handleLogout(); }}>
              <LogOut size={18} style={{display:'inline', marginRight:4}}/> Logout ({fullName || email})
            </a>
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
