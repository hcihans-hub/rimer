import ws from 'k6/ws';
import { check, sleep } from 'k6';

export const options = {
  vus: 6000,
  duration: '30s',
};

export default function () {
  const res = ws.connect('ws://localhost:5000/ws', null, function (socket) {
    socket.on('open', () => {
      sleep(30);
    });
  });
  
  check(res, {
    'connected successfully': (r) => r && r.status === 101,
  });
}
