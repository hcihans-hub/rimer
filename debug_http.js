import http from 'k6/http';
import { Counter } from 'k6/metrics';

const status200 = new Counter('status_200');
const status400 = new Counter('status_400');
const status401 = new Counter('status_401');
const status429 = new Counter('status_429');
const status500 = new Counter('status_500');
const statusOther = new Counter('status_other');

export const options = {
    scenarios: {
        debug_load: {
            executor: 'constant-arrival-rate',
            rate: 100,
            timeUnit: '1s',
            duration: '10s',
            preAllocatedVUs: 50,
            maxVUs: 500,
        },
    },
};

export default function () {
    const url = 'http://localhost:5000/api/audit';
    const payload = JSON.stringify({
        message: 'debug_test',
        priority: 'Low',
    });
    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    if (res.status === 200) status200.add(1);
    else if (res.status === 400) status400.add(1);
    else if (res.status === 401) status401.add(1);
    else if (res.status === 429) status429.add(1);
    else if (res.status === 500) status500.add(1);
    else statusOther.add(1);

    console.log(`Status: ${res.status} | Body: ${res.body}`);
}
