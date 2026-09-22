import ws from 'k6/ws';
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    websockets: {
      executor: 'constant-vus',
      vus: 1000,
      duration: '30s',
      exec: 'wsTest'
    },
    http_load: {
      executor: 'constant-arrival-rate',
      rate: 500,
      timeUnit: '1s',
      duration: '30s',
      preAllocatedVUs: 100,
      maxVUs: 1000,
      exec: 'httpTest'
    }
  }
};

export function wsTest() {
  ws.connect('ws://localhost:5000/ws', null, function (socket) {
    socket.on('open', () => {
      sleep(30);
    });
  });
}

export function httpTest() {
  const res = http.get('http://localhost:5000/api/system/status'); // Mock endpoint
  check(res, {
    'status is 200 or 404': (r) => r.status === 200 || r.status === 404,
  });
}
