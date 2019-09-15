module.exports = async function (context, req) {
    context.log('WarmUp function JavaScript function invoked.');
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: "Hello warmup js"
        };
};