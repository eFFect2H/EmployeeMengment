const API_URL = '/api';  // const API_URL = 'https://192.168.1.100:7118/api'; Укажите IP-адрес ПК 1

async function fetchWithToken(url, options = {}) {
    let currentToken = localStorage.getItem('token');
    if (!currentToken || currentToken === 'undefined') {
        console.error('Токен отсутствует или некорректен:', currentToken);
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error('Нет токена. Пожалуйста, авторизуйтесь.');
    }

    console.log('Текущий токен:', currentToken);

    let decodedToken;
    try {
        decodedToken = jwt_decode(currentToken);
        console.log('Декодированный токен:', decodedToken);
    } catch (e) {
        console.error('Ошибка декодирования токена:', e);
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error('Некорректный токен.');
    }

    const currentTime = Date.now() / 1000;
    if (decodedToken.exp < currentTime + 60) {
        currentToken = await refreshAccessToken();
    }

    options.headers = {
        ...options.headers,
        'Authorization': `Bearer ${currentToken}`
    };

    const response = await fetch(`${API_URL}${url}`, options);
    if (!response.ok) {
        if (response.status === 401) {
            localStorage.removeItem('token');
            localStorage.removeItem('refreshToken');
            window.location.href = 'login.html';
        }
        throw new Error(`Ошибка запроса: ${response.status}`);
    }
    return response.json();
}

async function refreshAccessToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error('Refresh-токен отсутствует.');
    }

    const response = await fetch(`${API_URL}/Account/Refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ RefreshToken: refreshToken })
    });

    if (!response.ok) {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error('Не удалось обновить токен.');
    }

    const data = await response.json();
    localStorage.setItem('token', data.AccessToken);
    return data.AccessToken;
}