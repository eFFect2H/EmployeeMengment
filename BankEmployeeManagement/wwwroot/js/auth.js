const API_URL = '/api';  // const API_URL = 'https://192.168.1.100:7118/api'; Укажите IP-адрес ПК 1

async function fetchWithToken(url, options = {}) {
    let currentToken = localStorage.getItem('token');
    if (!currentToken) {
        console.error('Токен отсутствует');
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error('Нет токена. Пожалуйста, авторизуйтесь.');
    }

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
    console.log('exp:', decodedToken.exp, 'Текущее время + 600:', currentTime + 600);
    if (decodedToken.exp < currentTime + 600) {
        console.log('Токен скоро истечет, обновляем...');
        currentToken = await refreshAccessToken();
    }

    options.headers = {
        ...options.headers,
        'Authorization': `Bearer ${currentToken}`
    };

    const response = await fetch(`${API_URL}${url}`, options);
    if (!response.ok) {
        if (response.status === 401) {
            console.log('Получен 401, пытаемся обновить токен...');
            try {
                currentToken = await refreshAccessToken();
                console.log('Новый токен получен:', currentToken);
                options.headers['Authorization'] = `Bearer ${currentToken}`;
                const retryResponse = await fetch(`${API_URL}${url}`, options);
                if (retryResponse.ok) return retryResponse.json();
            } catch (e) {
                console.error('Ошибка при обновлении токена после 401:', e);
                const errorText = e.message || 'Неизвестная ошибка';
                localStorage.removeItem('token');
                localStorage.removeItem('refreshToken');
                window.location.href = 'login.html';
                throw new Error(`Ошибка авторизации: ${errorText}`);
            }
        } else if (response.status === 400) {
            const errorText = await response.text();
            console.error('Ошибка валидации от сервера:', errorText);
            throw new Error(`Ошибка валидации: ${errorText}`);
        }
        throw new Error(`Ошибка запроса: ${response.status}`);
    }
    return response.json();
}

async function refreshAccessToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken || refreshToken.trim() === '') {
        console.error('Refresh-токен отсутствует или пустой');
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error('Refresh-токен отсутствует.');
    }

    console.log('Отправляемый RefreshToken:', refreshToken);
    const response = await fetch(`${API_URL}/Account/Refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ RefreshToken: refreshToken })
    });

    if (!response.ok) {
        const errorText = await response.text();
        console.error('Ошибка обновления токена:', errorText, 'Статус:', response.status);
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        window.location.href = 'login.html';
        throw new Error(`Не удалось обновить токен: ${errorText}`);
    }

    const data = await response.json();
    console.log('Новый AccessToken:', data.AccessToken);
    localStorage.setItem('token', data.AccessToken);
    return data.AccessToken;
}

// Проверка токена при загрузке
window.onload = async function () {
    try {
        const token = localStorage.getItem('token');
        if (token) {
            const decoded = jwt_decode(token);
            const currentTime = Date.now() / 1000;
            if (decoded.exp < currentTime + 600) {
                console.log('Токен скоро истечет при загрузке, обновляем...');
                await refreshAccessToken();
            }
        }
    } catch (e) {
        console.error('Ошибка при проверке токена на старте:', e);
    }
};