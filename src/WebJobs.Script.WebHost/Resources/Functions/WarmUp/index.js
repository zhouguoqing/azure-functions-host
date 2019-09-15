module.exports = function (context, req) {
    context.log('Node.js HTTP trigger warmup function processed a request');

    var res = {
        status: 200,
        body: 'hello js warmup'
    };
    context.done(null, res);
};
