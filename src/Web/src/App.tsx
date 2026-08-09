import { HealthStatus } from './features/health/HealthStatus';
import { LoginButton } from './features/auth/LoginButton';

function App() {
  return (
    <div>
      <h1>Enterprise Knowledge Hub</h1>
      <LoginButton />
      <HealthStatus />
    </div>
  );
}

export default App
