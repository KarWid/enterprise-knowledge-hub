import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { apiScopes } from '../../auth/msalConfig';

export function LoginButton() {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const handleLogin = () => {
    instance.loginRedirect({ scopes: apiScopes });
  };

  const handleLogout = () => {
    instance.logoutRedirect({ account: accounts[0] });
  };

  if (isAuthenticated) {
    return (
      <div>
        <span>{accounts[0]?.name ?? accounts[0]?.username}</span>
        {' '}
        <button onClick={handleLogout}>Log out</button>
      </div>
    );
  }

  return <button onClick={handleLogin}>Log in</button>;
}
