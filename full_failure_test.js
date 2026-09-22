import http from 'k6/http';
import ws from 'k6/ws';
import { check } from 'k6';

export const options = {
    scenarios: {
        http_load: {
            executor: 'ramping-arrival-rate',
            startRate: 200,
            timeUnit: '1s',
            preAllocatedVUs: 500,
            maxVUs: 5000,
            stages: [
                { target: 200, duration: '10s' },
                { target: 1000, duration: '20s' },
                { target: 3000, duration: '30s' },
            ],
            exec: 'http_test',
        },
        ws_load: {
            executor: 'ramping-vus',
            startVUs: 200,
            stages: [
                { target: 200, duration: '10s' },
                { target: 1000, duration: '20s' },
                { target: 2000, duration: '30s' },
            ],
            exec: 'ws_test',
        },
    },
    thresholds: {
        'http_req_duration': ['p(95)<500'],
    }
};

export function http_test() {
    const url = 'http://localhost:5000/api/audit';
    const payload = JSON.stringify({
        message: 'stress_test',
        priority: 'Low',
    });
    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    check(res, {
        'status is 200 or dropped': (r) => r.status === 200 || r.status === 429 || r.status === 503,
    });
}

export function ws_test() {
    const url = 'ws://localhost:5000/ws';

    const res = ws.connect(url, null, function (socket) {
        socket.on('open', function () {
            socket.setInterval(function timeout() {
                socket.ping();
            }, 10000);
        });

        socket.on('close', function () {});
        socket.on('error', function (e) {});

        socket.setTimeout(function () {
            socket.close();
        }, 65000); 
    });

    check(res, { 'status is 101': (r) => r && r.status === 101 });
}
