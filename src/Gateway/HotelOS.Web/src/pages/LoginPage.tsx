import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { LogIn, UserPlus } from 'lucide-react';

export default function LoginPage() {
  const [isLogin, setIsLogin] = useState(true);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [nationalId, setNationalId] = useState('');
  const [error, setError] = useState('');
  
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    const url = isLogin 
      ? 'http://localhost:5001/api/auth/login'
      : 'http://localhost:5001/api/auth/register';

    const payload = isLogin
      ? { email, password }
      : { fullName, email, phoneNumber: phone, nationalId, password };

    try {
      const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (res.ok) {
        localStorage.setItem('guestEmail', email);
        window.location.href = '/my-reservations';
      } else {
        const err = await res.json();
        setError(err.error || 'Authentication failed');
      }
    } catch (err) {
      setError('Network error. Is the server running?');
    }
  };

  return (
    <div className="container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '70vh' }}>
      <div className="glass-card" style={{ maxWidth: '400px', width: '100%', textAlign: 'center' }}>
        
        <div style={{ display: 'flex', marginBottom: 24, borderBottom: '1px solid var(--glass-border)' }}>
          <button 
            style={{ flex: 1, padding: 12, background: 'none', border: 'none', color: isLogin ? '#3b82f6' : 'white', borderBottom: isLogin ? '2px solid #3b82f6' : 'none', fontWeight: 600, cursor: 'pointer' }}
            onClick={() => setIsLogin(true)}
          >
            Login
          </button>
          <button 
            style={{ flex: 1, padding: 12, background: 'none', border: 'none', color: !isLogin ? '#3b82f6' : 'white', borderBottom: !isLogin ? '2px solid #3b82f6' : 'none', fontWeight: 600, cursor: 'pointer' }}
            onClick={() => setIsLogin(false)}
          >
            Register
          </button>
        </div>

        {isLogin ? <LogIn size={48} color="#3b82f6" style={{ marginBottom: 20 }} /> : <UserPlus size={48} color="#3b82f6" style={{ marginBottom: 20 }} />}
        
        <h2>{isLogin ? 'Welcome Back' : 'Create Account'}</h2>
        <p style={{ marginBottom: 24 }}>
          {isLogin ? 'Enter your credentials to access your account.' : 'Register to book rooms and manage stays.'}
        </p>
        
        {error && <div style={{ background: 'rgba(239, 68, 68, 0.2)', color: '#fca5a5', padding: '10px', borderRadius: '8px', marginBottom: '16px', fontSize: '0.9rem' }}>{error}</div>}

        <form onSubmit={handleSubmit} style={{ textAlign: 'left' }}>
          {!isLogin && (
            <>
              <input type="text" placeholder="Full Name" className="input-field" value={fullName} onChange={e => setFullName(e.target.value)} required style={{ marginBottom: 12 }} />
              <input type="text" placeholder="Phone Number" className="input-field" value={phone} onChange={e => setPhone(e.target.value)} required style={{ marginBottom: 12 }} />
              <input type="text" placeholder="National ID / Passport" className="input-field" value={nationalId} onChange={e => setNationalId(e.target.value)} required style={{ marginBottom: 12 }} />
            </>
          )}
          <input type="email" placeholder="Email Address" className="input-field" value={email} onChange={e => setEmail(e.target.value)} required style={{ marginBottom: 12 }} />
          <input type="password" placeholder="Password" className="input-field" value={password} onChange={e => setPassword(e.target.value)} required style={{ marginBottom: 24 }} />
          
          <button type="submit" className="btn-primary" style={{ width: '100%' }}>
            {isLogin ? 'Secure Login' : 'Register & Continue'}
          </button>
        </form>
      </div>
    </div>
  );
}
