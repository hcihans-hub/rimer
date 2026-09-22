import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

export const successRate = new Rate('connection_success_rate');
export const failureRate = new Rate('connection_failure_rate');

export const options = {
  stages: [
    { duration: '10s', target: 200 },
    { duration: '0s', target: 4000 },
    { duration: '20s', target: 4000 },
    { duration: '0s', target: 7000 },
    { duration: '20s', target: 7000 },
    { duration: '0s', target: 500 },
    { duration: '20s', target: 500 },
  ],
};

export default function () {
  const res = ws.connect('ws://localhost:5000/ws', null, function (socket) {
    socket.on('open', () => {
      sleep(70);
    });
  });

  const success = res && res.status === 101;
  successRate.add(success);
  failureRate.add(!success);

  check(res, {
    'connected successfully': (r) => r && r.status === 101,
  });
}
